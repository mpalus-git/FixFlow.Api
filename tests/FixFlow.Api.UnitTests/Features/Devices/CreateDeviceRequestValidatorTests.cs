using FixFlow.Api.Features.Devices.CreateDevice;
using Microsoft.Extensions.Time.Testing;

namespace FixFlow.Api.UnitTests.Features.Devices;

public sealed class CreateDeviceRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 23, 30, 0, TimeSpan.Zero);
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

    private static CreateDeviceRequest ValidRequest() =>
        new(Guid.CreateVersion7(), "AC-1001", "Split 3.5 kW", "Daikin", new DateOnly(2024, 5, 20));
}
