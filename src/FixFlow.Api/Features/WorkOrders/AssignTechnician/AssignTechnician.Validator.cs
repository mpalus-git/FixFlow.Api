using FluentValidation;

namespace FixFlow.Api.Features.WorkOrders.AssignTechnician;

public sealed class AssignTechnicianRequestValidator : AbstractValidator<AssignTechnicianRequest>
{
    public AssignTechnicianRequestValidator()
    {
        RuleFor(request => request.TechnicianId).NotEmpty();
    }
}
