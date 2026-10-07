using System.ComponentModel;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrderEvents;

[Description("Change recorded in the history of a work order.")]
public sealed record WorkOrderEventResponse(
    [property: Description("Identifier of the event.")] Guid Id,
    [property: Description("Kind of the change.")] WorkOrderEventType Type,
    [property: Description("UTC time of the change; for start and completion the time given by the technician.")] DateTimeOffset OccurredAt,
    [property: Description("Identifier of the user who made the change; null for changes made by the system, for example demo data.")] Guid? ActorId,
    [property: Description("First and last name of the user who made the change; null for changes made by the system.")] string? ActorName,
    [property: Description("Identifier of the technician assigned after the change; null when no technician is assigned.")] Guid? TechnicianId,
    [property: Description("First and last name of the technician assigned after the change; null when no technician is assigned.")] string? TechnicianName,
    [property: Description("UTC deadline of the work order after the change.")] DateTimeOffset DueDate);
