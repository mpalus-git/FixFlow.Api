using FluentValidation;

namespace FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;

public sealed class UpdateWorkOrderRequestValidator : AbstractValidator<UpdateWorkOrderRequest>
{
    public UpdateWorkOrderRequestValidator()
    {
        RuleFor(request => request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.Priority).IsInEnum();
        RuleFor(request => request.DueDate).NotEmpty();
    }
}
