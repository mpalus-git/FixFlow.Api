using System.ComponentModel;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.Clients;

namespace FixFlow.Api.Features.WorkOrders;

[Description("Service work order with basic details of its device, client and technician.")]
public sealed record WorkOrderResponse(
    [property: Description("Identifier of the work order.")] Guid Id,
    [property: Description("Readable number of the work order, for example ZL/2026/0042, numbered from 1 in each year of creation in the Europe/Warsaw time zone.")] string Number,
    [property: Description("Identifier of the serviced device.")] Guid DeviceId,
    [property: Description("Serial number of the serviced device.")] string DeviceSerialNumber,
    [property: Description("Model of the serviced device.")] string DeviceModel,
    [property: Description("Identifier of the client owning the device.")] Guid ClientId,
    [property: Description("Name of the client owning the device.")] string ClientName,
    [property: Description("Postal address of the client, where the device is serviced.")] ClientAddress ClientAddress,
    [property: Description("Contact person at the client.")] string ClientContactPerson,
    [property: Description("Contact phone number of the client.")] string ClientPhone,
    [property: Description("Description of the fault.")] string Description,
    [property: Description("Priority of the work order.")] WorkOrderPriority Priority,
    [property: Description("Current status. Allowed transitions: New -> Assigned -> InProgress -> Completed -> Invoiced, and Assigned -> New when the technician is unassigned.")] WorkOrderStatus Status,
    [property: Description("Identifier of the assigned technician; null when no technician is assigned.")] Guid? TechnicianId,
    [property: Description("Email of the assigned technician; null when no technician is assigned.")] string? TechnicianEmail,
    [property: Description("First and last name of the assigned technician; null when no technician is assigned.")] string? TechnicianName,
    [property: Description("UTC deadline of the work order.")] DateTimeOffset DueDate,
    [property: Description("True when the deadline has passed and the work order is neither completed nor invoiced. Set by an hourly job and recalculated immediately when the deadline changes.")] bool IsOverdue,
    [property: Description("UTC time when the work order was created.")] DateTimeOffset CreatedAt,
    [property: Description("UTC time when the technician started the work; null before the work is started.")] DateTimeOffset? StartedAt,
    [property: Description("UTC time when the work order was completed; null before completion.")] DateTimeOffset? CompletedAt,
    [property: Description("UTC time when the work order was invoiced; null before invoicing.")] DateTimeOffset? InvoicedAt,
    [property: Description("Identifier of the photo of the client's signature given at completion, available at /api/v1/photos/{photoId}; null when no signature was given.")] Guid? ClientSignaturePhotoId)
{
    public static WorkOrderResponse FromRow(WorkOrderRow row) => new(
        row.WorkOrder.Id,
        row.WorkOrder.Number,
        row.WorkOrder.DeviceId,
        row.DeviceSerialNumber,
        row.DeviceModel,
        row.ClientId,
        row.ClientName,
        ClientAddress.FromDomain(row.ClientAddress),
        row.ClientContactPerson,
        row.ClientPhone,
        row.WorkOrder.Description,
        row.WorkOrder.Priority,
        row.WorkOrder.Status,
        row.WorkOrder.TechnicianId,
        row.TechnicianEmail,
        row.TechnicianName,
        row.WorkOrder.DueDate,
        row.WorkOrder.IsOverdue,
        row.WorkOrder.CreatedAt,
        row.WorkOrder.StartedAt,
        row.WorkOrder.CompletedAt,
        row.WorkOrder.InvoicedAt,
        row.WorkOrder.ClientSignaturePhotoId);
}

public static class WorkOrderResponses
{
    public static Versioned<WorkOrderResponse> VersionedResponse(this FixFlowDbContext dbContext, WorkOrderRow row) =>
        dbContext.Versioned(row.WorkOrder, WorkOrderResponse.FromRow(row));

    public static async Task<Versioned<WorkOrderResponse>> VersionedWorkOrderResponseAsync(
        this FixFlowDbContext dbContext,
        WorkOrder workOrder,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.WorkOrders.FindRowAsync(workOrder.Id, dbContext, cancellationToken)
            ?? throw new InvalidOperationException($"Work order {workOrder.Id} was not found after saving.");

        return dbContext.VersionedResponse(row);
    }
}
