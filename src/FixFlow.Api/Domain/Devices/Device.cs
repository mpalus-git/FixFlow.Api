using ErrorOr;
using FixFlow.Api.Domain.Clients;

namespace FixFlow.Api.Domain.Devices;

public sealed class Device
{
    private Device()
    {
    }

    public Guid Id { get; private set; }

    public Guid ClientId { get; private set; }

    public string SerialNumber { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public string Manufacturer { get; private set; } = string.Empty;

    public DateOnly InstallationDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsArchived => ArchivedAt is not null;

    public static ErrorOr<Device> Create(
        Client client,
        string serialNumber,
        string model,
        string manufacturer,
        DateOnly installationDate,
        DateTimeOffset now)
    {
        if (client.IsArchived)
        {
            return DeviceErrors.ClientArchived;
        }

        return new Device
        {
            Id = Guid.CreateVersion7(),
            ClientId = client.Id,
            SerialNumber = NormalizeSerialNumber(serialNumber),
            Model = model,
            Manufacturer = manufacturer,
            InstallationDate = installationDate,
            CreatedAt = now,
        };
    }

    public static string NormalizeSerialNumber(string serialNumber) => serialNumber.Trim().ToUpperInvariant();

    public ErrorOr<Updated> Update(string serialNumber, string model, string manufacturer, DateOnly installationDate)
    {
        if (IsArchived)
        {
            return DeviceErrors.Archived;
        }

        SerialNumber = NormalizeSerialNumber(serialNumber);
        Model = model;
        Manufacturer = manufacturer;
        InstallationDate = installationDate;

        return Result.Updated;
    }

    public void Archive(DateTimeOffset now)
    {
        ArchivedAt ??= now;
    }
}
