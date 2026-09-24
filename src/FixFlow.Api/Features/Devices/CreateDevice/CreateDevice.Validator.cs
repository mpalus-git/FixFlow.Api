using FluentValidation;

namespace FixFlow.Api.Features.Devices.CreateDevice;

public sealed class CreateDeviceRequestValidator : AbstractValidator<CreateDeviceRequest>
{
    public CreateDeviceRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(request => request.ClientId).NotEmpty();
        Include(new DeviceDetailsValidator(timeProvider));
    }
}
