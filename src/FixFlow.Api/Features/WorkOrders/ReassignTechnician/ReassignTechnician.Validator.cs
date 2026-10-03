using FluentValidation;

namespace FixFlow.Api.Features.WorkOrders.ReassignTechnician;

public sealed class ReassignTechnicianRequestValidator : AbstractValidator<ReassignTechnicianRequest>
{
    public ReassignTechnicianRequestValidator()
    {
        RuleFor(request => request.TechnicianId).NotEmpty();
    }
}
