using FluentValidation;

namespace FixFlow.Api.Features.WorkOrders.CreateWorkOrder;

public sealed class CreateWorkOrderRequestValidator : AbstractValidator<CreateWorkOrderRequest>
{
    public CreateWorkOrderRequestValidator()
    {
        RuleFor(request => request.DeviceId).NotEmpty();
        RuleFor(request => request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.DueDate).NotEmpty();
        RuleFor(request => request.Priority).IsInEnum();
    }
}
