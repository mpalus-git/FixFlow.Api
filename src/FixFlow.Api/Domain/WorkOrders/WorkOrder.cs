using System.Linq.Expressions;
using ErrorOr;
using FixFlow.Api.Domain.Devices;

namespace FixFlow.Api.Domain.WorkOrders;

public sealed class WorkOrder
{
    private readonly List<WorkOrderEvent> _pendingEvents = [];

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

    public Guid? ClientSignaturePhotoId { get; private set; }

    public bool IsOverdue { get; private set; }

    public IReadOnlyList<WorkOrderEvent> PendingEvents => _pendingEvents;

    public static Expression<Func<WorkOrder, bool>> IsPastDueAt(DateTimeOffset now) =>
        workOrder => workOrder.DueDate < now
            && workOrder.Status != WorkOrderStatus.Completed
            && workOrder.Status != WorkOrderStatus.Invoiced;

    public static ErrorOr<WorkOrder> Create(
        Device device,
        string description,
        WorkOrderPriority priority,
        DateTimeOffset dueDate,
        DateTimeOffset now,
        Guid? actorId = null)
    {
        if (device.IsArchived)
        {
            return WorkOrderErrors.DeviceArchived;
        }

        if (dueDate <= now)
        {
            return WorkOrderErrors.DueDateNotInFuture;
        }

        var workOrder = new WorkOrder
        {
            Id = Guid.CreateVersion7(),
            DeviceId = device.Id,
            Description = description,
            Priority = priority,
            DueDate = dueDate,
            Status = WorkOrderStatus.New,
            CreatedAt = now,
        };
        workOrder.Record(WorkOrderEventType.Created, now, actorId);

        return workOrder;
    }

    public IReadOnlyList<WorkOrderEvent> TakePendingEvents()
    {
        var events = _pendingEvents.ToList();
        _pendingEvents.Clear();
        return events;
    }

    public void AssignNumber(string number)
    {
        if (Number.Length > 0)
        {
            throw new InvalidOperationException($"Work order {Id} already has number {Number}.");
        }

        Number = number;
    }

    public ErrorOr<Updated> Update(string description, WorkOrderPriority priority, DateTimeOffset dueDate, DateTimeOffset now, Guid? actorId = null)
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
        Record(WorkOrderEventType.Updated, now, actorId);

        return Result.Updated;
    }

    public ErrorOr<Updated> Assign(Guid technicianId, DateTimeOffset? dueDate, DateTimeOffset now, Guid? actorId = null)
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

        Record(WorkOrderEventType.Assigned, now, actorId);

        return Result.Updated;
    }

    public ErrorOr<Updated> Reassign(Guid technicianId, DateTimeOffset? dueDate, DateTimeOffset now, Guid? actorId = null)
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

        Record(WorkOrderEventType.Reassigned, now, actorId);

        return Result.Updated;
    }

    public ErrorOr<Updated> Unassign(DateTimeOffset now, Guid? actorId = null)
    {
        if (Status != WorkOrderStatus.Assigned)
        {
            return WorkOrderErrors.InvalidStatusTransition(Status, WorkOrderStatus.New);
        }

        TechnicianId = null;
        AssignedAt = null;
        Status = WorkOrderStatus.New;
        Record(WorkOrderEventType.Unassigned, now, actorId);

        return Result.Updated;
    }

    public ErrorOr<Updated> Start(
        Guid technicianId,
        bool technicianHasWorkInProgress,
        DateTimeOffset now,
        DateTimeOffset? requestedStartedAt = null,
        TimeSpan maxClockSkew = default)
    {
        if (Status != WorkOrderStatus.Assigned)
        {
            return IsStartedBy(technicianId)
                ? Result.Updated
                : WorkOrderErrors.InvalidStatusTransition(Status, WorkOrderStatus.InProgress);
        }

        if (TechnicianId != technicianId)
        {
            return WorkOrderErrors.NotAssignedToTechnician;
        }

        var startedAt = requestedStartedAt ?? now;
        if (startedAt < AssignedAt)
        {
            return WorkOrderErrors.StartedBeforeAssignment;
        }

        if (startedAt > now + maxClockSkew)
        {
            return WorkOrderErrors.StartedInFuture;
        }

        if (technicianHasWorkInProgress)
        {
            return WorkOrderErrors.TechnicianAlreadyHasWorkInProgress;
        }

        Status = WorkOrderStatus.InProgress;
        StartedAt = startedAt < now ? startedAt : now;
        Record(WorkOrderEventType.Started, StartedAt.Value, technicianId);

        return Result.Updated;
    }

    public ErrorOr<Updated> Complete(
        bool hasServiceEntries,
        DateTimeOffset now,
        DateTimeOffset? requestedCompletedAt = null,
        TimeSpan maxClockSkew = default,
        DateTimeOffset? lastWorkFinishedAt = null,
        ClientSignature? clientSignature = null,
        Guid? actorId = null)
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Invoiced)
        {
            return Result.Updated;
        }

        if (Status != WorkOrderStatus.InProgress)
        {
            return WorkOrderErrors.InvalidStatusTransition(Status, WorkOrderStatus.Completed);
        }

        if (!hasServiceEntries)
        {
            return WorkOrderErrors.NoServiceEntries;
        }

        var completedAt = requestedCompletedAt ?? now;
        if (requestedCompletedAt is { } requested)
        {
            if (requested < StartedAt)
            {
                return WorkOrderErrors.CompletedBeforeStart;
            }

            if (requested < lastWorkFinishedAt)
            {
                return WorkOrderErrors.CompletedBeforeWorkFinished;
            }

            if (requested > now + maxClockSkew)
            {
                return WorkOrderErrors.CompletedInFuture;
            }
        }

        if (clientSignature is not null && (clientSignature.UploadedByTechnicianId != TechnicianId || !clientSignature.IsReadableImage))
        {
            return WorkOrderErrors.InvalidClientSignature;
        }

        Status = WorkOrderStatus.Completed;
        CompletedAt = completedAt < now ? completedAt : now;
        ClientSignaturePhotoId = clientSignature?.PhotoId;
        Record(WorkOrderEventType.Completed, CompletedAt.Value, actorId);
        IsOverdue = false;

        return Result.Updated;
    }

    public ErrorOr<Updated> Invoice(DateTimeOffset now, Guid? actorId = null)
    {
        if (Status != WorkOrderStatus.Completed)
        {
            return WorkOrderErrors.InvalidStatusTransition(Status, WorkOrderStatus.Invoiced);
        }

        Status = WorkOrderStatus.Invoiced;
        InvoicedAt = now;
        Record(WorkOrderEventType.Invoiced, now, actorId);

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

    private void Record(WorkOrderEventType type, DateTimeOffset occurredAt, Guid? actorId) =>
        _pendingEvents.Add(WorkOrderEvent.Of(this, type, occurredAt, actorId));

    private bool IsStartedBy(Guid technicianId) =>
        TechnicianId == technicianId
        && Status is WorkOrderStatus.InProgress or WorkOrderStatus.Completed or WorkOrderStatus.Invoiced;

    private bool IsAcceptableDueDate(DateTimeOffset dueDate, DateTimeOffset now) => dueDate == DueDate || dueDate > now;

    private void ChangeDueDate(DateTimeOffset dueDate, DateTimeOffset now)
    {
        DueDate = dueDate;
        IsOverdue = dueDate < now;
    }
}
