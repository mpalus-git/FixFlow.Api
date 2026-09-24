using System.ComponentModel;

namespace FixFlow.Api.Features.Devices.UpdateDevice;

[Description("New data of an existing device. All fields are replaced; the owning client cannot be changed.")]
public sealed record UpdateDeviceRequest(
    [property: Description("Serial number, unique across all devices regardless of letter case and surrounding spaces.")] string SerialNumber,
    [property: Description("Device model.")] string Model,
    [property: Description("Device manufacturer.")] string Manufacturer,
    [property: Description("Calendar date when the device was installed; cannot be in the future.")] DateOnly InstallationDate) : IDeviceDetails;
