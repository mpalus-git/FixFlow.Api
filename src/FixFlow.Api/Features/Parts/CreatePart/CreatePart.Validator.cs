using FluentValidation;

namespace FixFlow.Api.Features.Parts.CreatePart;

public sealed class CreatePartRequestValidator : AbstractValidator<CreatePartRequest>
{
    public const int MaxInitialStockQuantity = 100_000;

    public CreatePartRequestValidator()
    {
        Include(new PartDetailsValidator());
        RuleFor(request => request.StockQuantity).InclusiveBetween(0, MaxInitialStockQuantity);
    }
}
