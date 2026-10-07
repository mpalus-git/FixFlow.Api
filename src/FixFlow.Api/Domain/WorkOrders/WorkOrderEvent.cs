namespace FixFlow.Api.Domain.WorkOrders;

public sealed class WorkOrderEvent
{
    private WorkOrderEvent()
    {
    }

    public Guid Id { get; private set; }

    public Guid WorkOrderId { get; private set; }

    public WorkOrderEventType Type { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid? ActorId { get; private set; }

    public Guid? TechnicianId { get; private set; }

    public DateTimeOffset DueDate { get; private set; }

    public static WorkOrderEvent Of(WorkOrder workOrder, WorkOrderEventType type, DateTimeOffset occurredAt, Guid? actorId) => new()
    {
        Id = Guid.CreateVersion7(),
        WorkOrderId = workOrder.Id,
        Type = type,
        OccurredAt = occurredAt,
        ActorId = actorId,
        TechnicianId = workOrder.TechnicianId,
        DueDate = workOrder.DueDate,
    };
}
