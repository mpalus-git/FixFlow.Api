using FluentValidation;

namespace FixFlow.Api.Features.ServiceEntries.AddServiceEntry;

public sealed class AddServiceEntryRequestValidator : AbstractValidator<AddServiceEntryRequest>
{
    public const int MaxPhotoCount = 10;
    public const int MaxPartCount = 20;
    public const int MaxPartQuantity = 1000;

    public AddServiceEntryRequestValidator()
    {
        RuleFor(request => request.Note).NotEmpty().MaximumLength(4000);
        RuleFor(request => request.Id).NotEqual(Guid.Empty);
        RuleFor(request => request.PhotoUrls)
            .Must(photoUrls => photoUrls is null || photoUrls.Count <= MaxPhotoCount)
            .WithMessage($"At most {MaxPhotoCount} photos can be attached.");
        RuleForEach(request => request.PhotoUrls)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(BeAbsoluteHttpUrl)
            .WithMessage("Photo URL must be an absolute http or https address.");
        RuleFor(request => request.Parts)
            .Must(parts => parts is null || parts.Count <= MaxPartCount)
            .WithMessage($"At most {MaxPartCount} parts can be listed.")
            .Must(parts => parts is null || parts.DistinctBy(part => part.PartId).Count() == parts.Count)
            .WithMessage("Each part can be listed only once.");
        RuleForEach(request => request.Parts).ChildRules(part =>
        {
            part.RuleFor(item => item.PartId).NotEmpty();
            part.RuleFor(item => item.Quantity).InclusiveBetween(1, MaxPartQuantity);
        });

        When(request => request.IsCorrection, () =>
        {
            RuleFor(request => request.Parts).NotEmpty().WithMessage("A correction must return at least one part.");
            RuleFor(request => request.WorkStartedAt).Null().WithMessage("A correction cannot have a work time.");
            RuleFor(request => request.WorkFinishedAt).Null().WithMessage("A correction cannot have a work time.");
            RuleFor(request => request.Latitude).Null().WithMessage("A correction cannot have a location.");
            RuleFor(request => request.Longitude).Null().WithMessage("A correction cannot have a location.");
        }).Otherwise(() =>
        {
            RuleFor(request => request.WorkStartedAt).NotNull();
            RuleFor(request => request.WorkFinishedAt).NotNull();
            RuleFor(request => request.Latitude)
                .NotNull()
                .When(request => request.Longitude is not null)
                .WithMessage("Latitude and longitude must be given together.")
                .InclusiveBetween(-90, 90);
            RuleFor(request => request.Longitude)
                .NotNull()
                .When(request => request.Latitude is not null)
                .WithMessage("Latitude and longitude must be given together.")
                .InclusiveBetween(-180, 180);
        });
    }

    private static bool BeAbsoluteHttpUrl(string photoUrl) =>
        Uri.TryCreate(photoUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
