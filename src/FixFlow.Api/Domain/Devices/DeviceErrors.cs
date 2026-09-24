using ErrorOr;

namespace FixFlow.Api.Domain.Devices;

public static class DeviceErrors
{
    public static readonly Error NotFound = Error.NotFound("Device.NotFound", "Device was not found.");

    public static readonly Error Archived = Error.Conflict("Device.Archived", "Archived device cannot be modified.");

    public static readonly Error ClientArchived = Error.Conflict("Device.ClientArchived", "Devices cannot be added to an archived client.");

    public static readonly Error DuplicateSerialNumber = Error.Conflict("Device.DuplicateSerialNumber", "A device with this serial number already exists.");
}
