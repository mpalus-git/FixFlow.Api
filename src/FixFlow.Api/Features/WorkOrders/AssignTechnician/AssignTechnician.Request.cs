using System.ComponentModel;

namespace FixFlow.Api.Features.WorkOrders.AssignTechnician;

[Description("Technician to assign to a work order.")]
public sealed record AssignTechnicianRequest(
    [property: Description("Identifier of a user with the Technician role.")] Guid TechnicianId);
