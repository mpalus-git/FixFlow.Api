using FluentValidation;

namespace FixFlow.Api.Features.Devices.UpdateDevice;

public sealed class UpdateDeviceRequestValidator : AbstractValidator<UpdateDeviceRequest>
{
    public UpdateDeviceRequestValidator(TimeProvider timeProvider)
    {
        Include(new DeviceDetailsValidator(timeProvider));
    }
}
