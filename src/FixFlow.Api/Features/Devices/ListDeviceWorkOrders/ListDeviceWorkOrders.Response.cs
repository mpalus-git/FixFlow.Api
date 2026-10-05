using System.ComponentModel;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.Devices.ListDeviceWorkOrders;

[Description("Work order in the service history of a device.")]
public sealed record DeviceWorkOrderHistoryItemResponse(
    [property: Description("Identifier of the work order.")] Guid Id,
    [property: Description("Readable number of the work order, for example ZL/2026/0042.")] string Number,
    [property: Description("Current status of the work order.")] WorkOrderStatus Status,
    [property: Description("Priority of the work order.")] WorkOrderPriority Priority,
    [property: Description("Description of the fault.")] string Description,
    [property: Description("First and last name of the assigned technician; null when no technician is assigned.")] string? TechnicianName,
    [property: Description("UTC time when the work order was created.")] DateTimeOffset CreatedAt,
    [property: Description("UTC time when the technician started the work; null before the work is started.")] DateTimeOffset? StartedAt,
    [property: Description("UTC time when the work order was completed; null before completion.")] DateTimeOffset? CompletedAt,
    [property: Description("Number of service entries of the work order, including correction entries.")] int ServiceEntryCount)
{
    public static DeviceWorkOrderHistoryItemResponse FromRow(DeviceWorkOrderHistoryRow row) => new(
        row.WorkOrder.Id,
        row.WorkOrder.Number,
        row.WorkOrder.Status,
        row.WorkOrder.Priority,
        row.WorkOrder.Description,
        row.TechnicianName,
        row.WorkOrder.CreatedAt,
        row.WorkOrder.StartedAt,
        row.WorkOrder.CompletedAt,
        row.ServiceEntryCount);
}

public sealed class DeviceWorkOrderHistoryRow
{
    public required WorkOrder WorkOrder { get; init; }

    public string? TechnicianName { get; init; }

    public required int ServiceEntryCount { get; init; }
}
