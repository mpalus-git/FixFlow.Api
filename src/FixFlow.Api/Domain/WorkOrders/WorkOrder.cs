using System.Linq.Expressions;
using ErrorOr;
using FixFlow.Api.Domain.Devices;

namespace FixFlow.Api.Domain.WorkOrders;

public sealed class WorkOrder
{
    private WorkOrder()
    {
    }

    public Guid Id { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public Guid DeviceId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public WorkOrderPriority Priority { get; private set; }

    public DateTimeOffset DueDate { get; private set; }

    public WorkOrderStatus Status { get; private set; }

    public Guid? TechnicianId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? AssignedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? InvoicedAt { get; private set; }

    public bool IsOverdue { get; private set; }

    public static Expression<Func<WorkOrder, bool>> IsPastDueAt(DateTimeOffset now) =>
        workOrder => workOrder.DueDate < now
            && workOrder.Status != WorkOrderStatus.Completed
            && workOrder.Status != WorkOrderStatus.Invoiced;

    public static ErrorOr<WorkOrder> Create(
        Device device,
        string description,
        WorkOrderPriority priority,
        DateTimeOffset dueDate,
        DateTimeOffset now)
    {
        if (device.IsArchived)
        {
            return WorkOrderErrors.DeviceArchived;
        }

        if (dueDate <= now)
        {
            return WorkOrderErrors.DueDateNotInFuture;
        }

        return new WorkOrder
        {
            Id = Guid.CreateVersion7(),
            DeviceId = device.Id,
            Description = description,
            Priority = priority,
            DueDate = dueDate,
            Status = WorkOrderStatus.New,
            CreatedAt = now,
        };
    }

    public void AssignNumber(string number)
    {
        if (Number.Length > 0)
        {
            throw new InvalidOperationException($"Work order {Id} already has number {Number}.");
        }

        Number = number;
    }

    public ErrorOr<Updated> Update(string description, WorkOrderPriority priority, DateTimeOffset dueDate, DateTimeOffset now)
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Invoiced)
        {
            return WorkOrderErrors.Closed;
        }

        if (!IsAcceptableDueDate(dueDate, now))
        {
            return WorkOrderErrors.DueDateNotInFuture;
        }

        Description = description;
        Priority = priority;
        ChangeDueDate(dueDate, now);

        return Result.Updated;
    }

    public ErrorOr<Updated> Assign(Guid technicianId, DateTimeOffset? dueDate, DateTimeOffset now)
    {
        if (Status != WorkOrderStatus.New)
        {
            return WorkOrderErrors.InvalidStatusTransition(Status, WorkOrderStatus.Assigned);
        }

        if (dueDate is { } newDueDate && !IsAcceptableDueDate(newDueDate, now))
        {
            return WorkOrderErrors.DueDateNotInFuture;
        }

        TechnicianId = technicianId;
        AssignedAt = now;
        Status = WorkOrderStatus.Assigned;
        if (dueDate is { } changedDueDate)
        {
            ChangeDueDate(changedDueDate, now);
        }

        return Result.Updated;
    }

    public ErrorOr<Updated> Reassign(Guid technicianId, DateTimeOffset? dueDate, DateTimeOffset now)
    {
        if (Status != WorkOrderStatus.Assigned)
        {
            return WorkOrderErrors.NotReassignable;
        }

        if (dueDate is { } newDueDate && !IsAcceptableDueDate(newDueDate, now))
        {
            return WorkOrderErrors.DueDateNotInFuture;
        }

        TechnicianId = technicianId;
        AssignedAt = now;
        if (dueDate is { } changedDueDate)
        {
            ChangeDueDate(changedDueDate, now);
        }

        return Result.Updated;
    }

    public ErrorOr<Updated> Unassign()
    {
        if (Status != WorkOrderStatus.Assigned)
        {
            return WorkOrderErrors.InvalidStatusTransition(Status, WorkOrderStatus.New);
        }

        TechnicianId = null;
        AssignedAt = null;
        Status = WorkOrderStatus.New;

        return Result.Updated;
    }

    public ErrorOr<Updated> Start(Guid technicianId, bool technicianHasWorkInProgress, DateTimeOffset now)
    {
        if (Status != WorkOrderStatus.Assigned)
        {
            return WorkOrderErrors.InvalidStatusTransition(Status, WorkOrderStatus.InProgress);
        }

        if (TechnicianId != technicianId)
        {
            return WorkOrderErrors.NotAssignedToTechnician;
        }

        if (technicianHasWorkInProgress)
        {
            return WorkOrderErrors.TechnicianAlreadyHasWorkInProgress;
        }

        Status = WorkOrderStatus.InProgress;
        StartedAt = now;

        return Result.Updated;
    }

    public ErrorOr<Updated> Complete(bool hasServiceEntries, DateTimeOffset now)
    {
        if (Status != WorkOrderStatus.InProgress)
        {
            return WorkOrderErrors.InvalidStatusTransition(Status, WorkOrderStatus.Completed);
        }

        if (!hasServiceEntries)
        {
            return WorkOrderErrors.NoServiceEntries;
        }

        Status = WorkOrderStatus.Completed;
        CompletedAt = now;
        IsOverdue = false;

        return Result.Updated;
    }

    public ErrorOr<Updated> Invoice(DateTimeOffset now)
    {
        if (Status != WorkOrderStatus.Completed)
        {
            return WorkOrderErrors.InvalidStatusTransition(Status, WorkOrderStatus.Invoiced);
        }

        Status = WorkOrderStatus.Invoiced;
        InvoicedAt = now;

        return Result.Updated;
    }

    public ErrorOr<Success> EnsureCanIssueServiceProtocol() =>
        Status is WorkOrderStatus.Completed or WorkOrderStatus.Invoiced ? Result.Success : WorkOrderErrors.NotCompleted;

    public ErrorOr<Success> EnsureCanAddServiceEntry(Guid technicianId)
    {
        if (Status != WorkOrderStatus.InProgress)
        {
            return WorkOrderErrors.NotInProgress;
        }

        if (TechnicianId != technicianId)
        {
            return WorkOrderErrors.NotAssignedToTechnician;
        }

        return Result.Success;
    }

    private bool IsAcceptableDueDate(DateTimeOffset dueDate, DateTimeOffset now) => dueDate == DueDate || dueDate > now;

    private void ChangeDueDate(DateTimeOffset dueDate, DateTimeOffset now)
    {
        DueDate = dueDate;
        IsOverdue = dueDate < now;
    }
}
