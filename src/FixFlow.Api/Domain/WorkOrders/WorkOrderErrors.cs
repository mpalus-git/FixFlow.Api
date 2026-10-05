using ErrorOr;

namespace FixFlow.Api.Domain.WorkOrders;

public static class WorkOrderErrors
{
    public static readonly Error NotFound = Error.NotFound("WorkOrder.NotFound", "Work order was not found.");

    public static readonly Error TechnicianNotFound = Error.NotFound("WorkOrder.TechnicianNotFound", "Active technician was not found.");

    public static readonly Error DeviceArchived = Error.Conflict("WorkOrder.DeviceArchived", "Work orders cannot be created for an archived device.");

    public static readonly Error DueDateNotInFuture = Error.Validation("DueDate", "Due date must be in the future.");

    public static readonly Error StartedBeforeAssignment = Error.Validation("StartedAt", "Work order cannot be started before it was assigned.");

    public static readonly Error StartedInFuture = Error.Validation("StartedAt", "Start time cannot be in the future.");

    public static readonly Error NotAssignedToTechnician = Error.Forbidden("WorkOrder.NotAssignedToTechnician", "Work order is not assigned to this technician.");

    public static readonly Error TechnicianAlreadyHasWorkInProgress = Error.Conflict(
        "WorkOrder.TechnicianAlreadyHasWorkInProgress",
        "Technician already has another work order in progress.");

    public static readonly Error NotInProgress = Error.Conflict(
        "WorkOrder.NotInProgress",
        "Service entries can be added only to a work order in progress.");

    public static readonly Error NoServiceEntries = Error.Conflict(
        "WorkOrder.NoServiceEntries",
        "Work order cannot be completed without at least one service entry.");

    public static readonly Error Closed = Error.Conflict(
        "WorkOrder.Closed",
        "Completed or invoiced work order cannot be modified.");

    public static readonly Error NotCompleted = Error.Conflict(
        "WorkOrder.NotCompleted",
        "Service protocol is available only for completed or invoiced work orders.");

    public static readonly Error NotReassignable = Error.Conflict(
        "WorkOrder.NotReassignable",
        "Only an assigned work order that has not been started can be reassigned.");

    public static Error InvalidStatusTransition(WorkOrderStatus from, WorkOrderStatus to) => Error.Conflict(
        "WorkOrder.InvalidStatusTransition",
        $"Work order status cannot change from {from} to {to}.");
}
