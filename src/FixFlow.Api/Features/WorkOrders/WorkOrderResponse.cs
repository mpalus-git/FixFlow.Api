using System.ComponentModel;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders;

[Description("Service work order for a device.")]
public sealed record WorkOrderResponse(
    [property: Description("Identifier of the work order.")] Guid Id,
    [property: Description("Identifier of the serviced device.")] Guid DeviceId,
    [property: Description("Description of the fault.")] string Description,
    [property: Description("Priority of the work order.")] WorkOrderPriority Priority,
    [property: Description("Current status. Allowed transitions: New -> Assigned -> InProgress -> Completed -> Invoiced, and Assigned -> New when the technician is unassigned.")] WorkOrderStatus Status,
    [property: Description("Identifier of the assigned technician; null when no technician is assigned.")] Guid? TechnicianId,
    [property: Description("UTC deadline of the work order.")] DateTimeOffset DueDate,
    [property: Description("True when the deadline has passed and the work order is neither completed nor invoiced. Set by an hourly job and recalculated immediately when the deadline changes.")] bool IsOverdue,
    [property: Description("UTC time when the work order was created.")] DateTimeOffset CreatedAt,
    [property: Description("UTC time when the technician started the work; null before the work is started.")] DateTimeOffset? StartedAt,
    [property: Description("UTC time when the work order was completed; null before completion.")] DateTimeOffset? CompletedAt,
    [property: Description("UTC time when the work order was invoiced; null before invoicing.")] DateTimeOffset? InvoicedAt)
{
    public static WorkOrderResponse FromDomain(WorkOrder workOrder) => new(
        workOrder.Id,
        workOrder.DeviceId,
        workOrder.Description,
        workOrder.Priority,
        workOrder.Status,
        workOrder.TechnicianId,
        workOrder.DueDate,
        workOrder.IsOverdue,
        workOrder.CreatedAt,
        workOrder.StartedAt,
        workOrder.CompletedAt,
        workOrder.InvoicedAt);
}
