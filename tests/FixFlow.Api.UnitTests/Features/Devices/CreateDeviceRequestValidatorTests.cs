using FixFlow.Api.Features.Devices.CreateDevice;
using System.Globalization;
using Microsoft.Extensions.Time.Testing;

namespace FixFlow.Api.UnitTests.Features.Devices;

public sealed class CreateDeviceRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 1);

    private readonly CreateDeviceRequestValidator _validator = new(new FakeTimeProvider(Now));

    [Fact]
    public void Should_Accept_Request_When_Installation_Date_Is_Today()
    {
        var result = _validator.Validate(ValidRequest() with { InstallationDate = Today });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Installation_Date_Is_In_The_Future()
    {
        var result = _validator.Validate(ValidRequest() with { InstallationDate = Today.AddDays(1) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateDeviceRequest.InstallationDate));
    }

    [Fact]
    public void Should_Reject_Request_When_Installation_Date_Is_Missing()
    {
        var result = _validator.Validate(ValidRequest() with { InstallationDate = default });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateDeviceRequest.InstallationDate));
    }

    [Fact]
    public void Should_Reject_Request_When_Client_Id_Is_Empty()
    {
        var result = _validator.Validate(ValidRequest() with { ClientId = Guid.Empty });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateDeviceRequest.ClientId));
    }

    [Fact]
    public void Should_Reject_Request_When_Serial_Number_Model_And_Manufacturer_Are_Empty()
    {
        var result = _validator.Validate(ValidRequest() with { SerialNumber = " ", Model = string.Empty, Manufacturer = string.Empty });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateDeviceRequest.SerialNumber));
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateDeviceRequest.Model));
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateDeviceRequest.Manufacturer));
    }

    [Fact]
    public void Should_Reject_Request_When_Serial_Number_Is_Longer_Than_One_Hundred_Characters()
    {
        var result = _validator.Validate(ValidRequest() with { SerialNumber = new string('A', 101) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateDeviceRequest.SerialNumber));
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

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateDeviceRequest.InstallationDate));
    }

    private static CreateDeviceRequestValidator ValidatorAt(string now) =>
        new(new FakeTimeProvider(DateTimeOffset.Parse(now, CultureInfo.InvariantCulture)));

    private static DateOnly ParseDate(string date) => DateOnly.Parse(date, CultureInfo.InvariantCulture);

    private static CreateDeviceRequest ValidRequest() =>
        new(Guid.CreateVersion7(), "AC-1001", "Split 3.5 kW", "Daikin", new DateOnly(2024, 5, 20));
}
