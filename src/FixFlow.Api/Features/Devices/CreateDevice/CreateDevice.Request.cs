using System.ComponentModel;

namespace FixFlow.Api.Features.Devices.CreateDevice;

[Description("Data of a new device.")]
public sealed record CreateDeviceRequest(
    [property: Description("Identifier of an active client that owns the device.")] Guid ClientId,
    [property: Description("Serial number, unique across all devices regardless of letter case and surrounding spaces.")] string SerialNumber,
    [property: Description("Device model.")] string Model,
    [property: Description("Device manufacturer.")] string Manufacturer,
    [property: Description("Calendar date when the device was installed; cannot be in the future in the Europe/Warsaw time zone.")] DateOnly InstallationDate) : IDeviceDetails;
