using FixFlow.Api.Common.Time;
using FluentValidation;

namespace FixFlow.Api.Features.Devices;

public interface IDeviceDetails
{
    string SerialNumber { get; }

    string Model { get; }

    string Manufacturer { get; }

    DateOnly InstallationDate { get; }
}

public sealed class DeviceDetailsValidator : AbstractValidator<IDeviceDetails>
{
    public DeviceDetailsValidator(TimeProvider timeProvider)
    {
        RuleFor(details => details.SerialNumber).NotEmpty().MaximumLength(100);
        RuleFor(details => details.Model).NotEmpty().MaximumLength(100);
        RuleFor(details => details.Manufacturer).NotEmpty().MaximumLength(100);
        RuleFor(details => details.InstallationDate)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(BusinessTime.From(timeProvider.GetUtcNow()).DateTime))
            .WithMessage("Installation date cannot be in the future.");
    }
}
