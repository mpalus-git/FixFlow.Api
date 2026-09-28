using FixFlow.Api.Features.Devices.UpdateDevice;
using Microsoft.Extensions.Time.Testing;

namespace FixFlow.Api.UnitTests.Features.Devices;

public sealed class UpdateDeviceRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 23, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 1);

    private readonly UpdateDeviceRequestValidator _validator = new(new FakeTimeProvider(Now));

    [Fact]
    public void Should_Accept_Request_When_Device_Details_Are_Valid()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Apply_Shared_Device_Rules_When_Serial_Number_Is_Empty_And_Installation_Date_Is_In_The_Future()
    {
        var result = _validator.Validate(ValidRequest() with { SerialNumber = " ", InstallationDate = Today.AddDays(1) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateDeviceRequest.SerialNumber));
        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateDeviceRequest.InstallationDate));
    }

    private static UpdateDeviceRequest ValidRequest() => new("AC-2002", "Multi 5 kW", "Mitsubishi", Today);
}
