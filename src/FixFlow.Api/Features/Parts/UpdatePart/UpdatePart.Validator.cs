using FluentValidation;

namespace FixFlow.Api.Features.Parts.UpdatePart;

public sealed class UpdatePartRequestValidator : AbstractValidator<UpdatePartRequest>
{
    public UpdatePartRequestValidator()
    {
        Include(new PartDetailsValidator());
    }
}
