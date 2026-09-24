using FluentValidation;

namespace FixFlow.Api.Features.Parts.RestockPart;

public sealed class RestockPartRequestValidator : AbstractValidator<RestockPartRequest>
{
    public const int MaxDeliveryQuantity = 100_000;

    public RestockPartRequestValidator()
    {
        RuleFor(request => request.Quantity).InclusiveBetween(1, MaxDeliveryQuantity);
    }
}
