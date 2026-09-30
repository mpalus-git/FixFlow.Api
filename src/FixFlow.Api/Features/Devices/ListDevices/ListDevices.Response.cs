using System.ComponentModel;
using FixFlow.Api.Domain.Devices;

namespace FixFlow.Api.Features.Devices.ListDevices;

[Description("Device on a list, with the name of the client that owns it.")]
public sealed record DeviceListItemResponse(
    [property: Description("Identifier of the device.")] Guid Id,
    [property: Description("Identifier of the client that owns the device.")] Guid ClientId,
    [property: Description("Name of the client that owns the device.")] string ClientName,
    [property: Description("Unique serial number, stored trimmed and in upper case.")] string SerialNumber,
    [property: Description("Device model.")] string Model,
    [property: Description("Device manufacturer.")] string Manufacturer,
    [property: Description("Calendar date when the device was installed.")] DateOnly InstallationDate,
    [property: Description("UTC time when the device was created.")] DateTimeOffset CreatedAt,
    [property: Description("UTC time when the device was archived; null for active devices.")] DateTimeOffset? ArchivedAt)
{
    public static DeviceListItemResponse FromRow(DeviceListRow row) => new(
        row.Device.Id,
        row.Device.ClientId,
        row.ClientName,
        row.Device.SerialNumber,
        row.Device.Model,
        row.Device.Manufacturer,
        row.Device.InstallationDate,
        row.Device.CreatedAt,
        row.Device.ArchivedAt);
}

public sealed class DeviceListRow
{
    public required Device Device { get; init; }

    public required string ClientName { get; init; }
}
