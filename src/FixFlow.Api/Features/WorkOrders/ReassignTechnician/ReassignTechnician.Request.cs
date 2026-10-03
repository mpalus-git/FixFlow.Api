using System.ComponentModel;

namespace FixFlow.Api.Features.WorkOrders.ReassignTechnician;

[Description("Technician and optional new deadline of an assigned work order.")]
public sealed record ReassignTechnicianRequest(
    [property: Description("Identifier of an active user with the Technician role; may be the currently assigned technician when only the deadline changes.")] Guid TechnicianId,
    [property: Description("New UTC deadline; it must be in the future when it differs from the current one. When omitted, the deadline does not change.")] DateTimeOffset? DueDate = null);
