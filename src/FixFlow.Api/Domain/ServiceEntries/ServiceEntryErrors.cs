using ErrorOr;

namespace FixFlow.Api.Domain.ServiceEntries;

public static class ServiceEntryErrors
{
    public static readonly Error NotFound = Error.NotFound("ServiceEntry.NotFound", "Service entry was not found.");

    public static readonly Error WorkTimeRequired = Error.Validation("WorkStartedAt", "A work entry requires work start and finish times.");

    public static readonly Error WorkStartedBeforeWorkOrder = Error.Validation("WorkStartedAt", "Work cannot start before the work order was started.");

    public static readonly Error WorkNotFinishedAfterStart = Error.Validation("WorkFinishedAt", "Work must finish after it starts.");

    public static readonly Error WorkFinishedInFuture = Error.Validation("WorkFinishedAt", "Work cannot finish in the future.");

    public static readonly Error ReturnExceedsConsumption = Error.Conflict(
        "ServiceEntry.ReturnExceedsConsumption",
        "Returned quantity exceeds the quantity of the part used on this work order.");

    public static readonly Error IdConflict = Error.Conflict(
        "ServiceEntry.IdConflict",
        "A service entry with this identifier already exists on another work order or was added by another technician.");
}
