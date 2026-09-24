using FluentValidation;

namespace FixFlow.Api.Features.Parts;

public interface IPartDetails
{
    string Name { get; }

    string CatalogNumber { get; }

    decimal UnitPrice { get; }
}

public sealed class PartDetailsValidator : AbstractValidator<IPartDetails>
{
    public PartDetailsValidator()
    {
        RuleFor(details => details.Name).NotEmpty().MaximumLength(200);
        RuleFor(details => details.CatalogNumber).NotEmpty().MaximumLength(50);
        RuleFor(details => details.UnitPrice).GreaterThanOrEqualTo(0).PrecisionScale(12, 2, true);
    }
}
