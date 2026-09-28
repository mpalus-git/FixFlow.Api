using System.ComponentModel;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrders;

[Description("Work order on a list, with basic details of its device, client and technician.")]
public sealed record WorkOrderListItemResponse(
    [property: Description("Identifier of the work order.")] Guid Id,
    [property: Description("Identifier of the serviced device.")] Guid DeviceId,
    [property: Description("Serial number of the serviced device.")] string DeviceSerialNumber,
    [property: Description("Model of the serviced device.")] string DeviceModel,
    [property: Description("Identifier of the client owning the device.")] Guid ClientId,
    [property: Description("Name of the client owning the device.")] string ClientName,
    [property: Description("Description of the fault.")] string Description,
    [property: Description("Priority of the work order.")] WorkOrderPriority Priority,
    [property: Description("Current status of the work order.")] WorkOrderStatus Status,
    [property: Description("Identifier of the assigned technician; null when no technician is assigned.")] Guid? TechnicianId,
    [property: Description("Email of the assigned technician; null when no technician is assigned.")] string? TechnicianEmail,
    [property: Description("UTC deadline of the work order.")] DateTimeOffset DueDate,
    [property: Description("True when the deadline has passed and the work order is neither completed nor invoiced.")] bool IsOverdue,
    [property: Description("UTC time when the work order was created.")] DateTimeOffset CreatedAt,
    [property: Description("UTC time when the technician started the work; null before the work is started.")] DateTimeOffset? StartedAt,
    [property: Description("UTC time when the work order was completed; null before completion.")] DateTimeOffset? CompletedAt,
    [property: Description("UTC time when the work order was invoiced; null before invoicing.")] DateTimeOffset? InvoicedAt)
{
    public static WorkOrderListItemResponse FromRow(WorkOrderListRow row) => new(
        row.WorkOrder.Id,
        row.WorkOrder.DeviceId,
        row.DeviceSerialNumber,
        row.DeviceModel,
        row.ClientId,
        row.ClientName,
        row.WorkOrder.Description,
        row.WorkOrder.Priority,
        row.WorkOrder.Status,
        row.WorkOrder.TechnicianId,
        row.TechnicianEmail,
        row.WorkOrder.DueDate,
        row.WorkOrder.IsOverdue,
        row.WorkOrder.CreatedAt,
        row.WorkOrder.StartedAt,
        row.WorkOrder.CompletedAt,
        row.WorkOrder.InvoicedAt);
}

public sealed class WorkOrderListRow
{
    public required WorkOrder WorkOrder { get; init; }

    public required string DeviceSerialNumber { get; init; }

    public required string DeviceModel { get; init; }

    public required Guid ClientId { get; init; }

    public required string ClientName { get; init; }

    public string? TechnicianEmail { get; init; }
}
