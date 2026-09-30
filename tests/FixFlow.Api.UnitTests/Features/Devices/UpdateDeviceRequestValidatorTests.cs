using FixFlow.Api.Features.Devices.UpdateDevice;
using System.Globalization;
using Microsoft.Extensions.Time.Testing;

namespace FixFlow.Api.UnitTests.Features.Devices;

public sealed class UpdateDeviceRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
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

    [Theory]
    [InlineData("2026-07-15T22:30:00Z", "2026-07-16")]
    [InlineData("2026-01-15T23:30:00Z", "2026-01-16")]
    public void Should_Accept_Installation_Date_Of_Today_In_Warsaw_When_Utc_Date_Is_Still_Previous_Day(string now, string installationDate)
    {
        var validator = ValidatorAt(now);

        var result = validator.Validate(ValidRequest() with { InstallationDate = ParseDate(installationDate) });

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("2026-07-15T22:30:00Z", "2026-07-17")]
    [InlineData("2026-01-15T23:30:00Z", "2026-01-17")]
    public void Should_Reject_Installation_Date_After_Today_In_Warsaw_When_Utc_Date_Is_Still_Previous_Day(string now, string installationDate)
    {
        var validator = ValidatorAt(now);

        var result = validator.Validate(ValidRequest() with { InstallationDate = ParseDate(installationDate) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateDeviceRequest.InstallationDate));
    }

    private static UpdateDeviceRequestValidator ValidatorAt(string now) =>
        new(new FakeTimeProvider(DateTimeOffset.Parse(now, CultureInfo.InvariantCulture)));

    private static DateOnly ParseDate(string date) => DateOnly.Parse(date, CultureInfo.InvariantCulture);

    private static UpdateDeviceRequest ValidRequest() => new("AC-2002", "Multi 5 kW", "Mitsubishi", Today);
}
