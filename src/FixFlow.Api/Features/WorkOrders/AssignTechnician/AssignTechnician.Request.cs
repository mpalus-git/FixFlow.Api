using System.ComponentModel;

namespace FixFlow.Api.Features.WorkOrders.AssignTechnician;

[Description("Technician to assign to a work order and an optional new deadline.")]
public sealed record AssignTechnicianRequest(
    [property: Description("Identifier of a user with the Technician role.")] Guid TechnicianId,
    [property: Description("New UTC deadline; it must be in the future when it differs from the current one. When omitted, the deadline does not change.")] DateTimeOffset? DueDate = null);
