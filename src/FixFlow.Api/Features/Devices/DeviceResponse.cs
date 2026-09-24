using System.ComponentModel;
using FixFlow.Api.Domain.Devices;

namespace FixFlow.Api.Features.Devices;

[Description("Device installed at a client and serviced by the company.")]
public sealed record DeviceResponse(
    [property: Description("Identifier of the device.")] Guid Id,
    [property: Description("Identifier of the client that owns the device.")] Guid ClientId,
    [property: Description("Unique serial number, stored trimmed and in upper case.")] string SerialNumber,
    [property: Description("Device model.")] string Model,
    [property: Description("Device manufacturer.")] string Manufacturer,
    [property: Description("Calendar date when the device was installed.")] DateOnly InstallationDate,
    [property: Description("UTC time when the device was created.")] DateTimeOffset CreatedAt,
    [property: Description("UTC time when the device was archived; null for active devices.")] DateTimeOffset? ArchivedAt)
{
    public static DeviceResponse FromDomain(Device device) => new(
        device.Id,
        device.ClientId,
        device.SerialNumber,
        device.Model,
        device.Manufacturer,
        device.InstallationDate,
        device.CreatedAt,
        device.ArchivedAt);
}
