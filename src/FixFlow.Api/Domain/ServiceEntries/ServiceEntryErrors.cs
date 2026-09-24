using ErrorOr;

namespace FixFlow.Api.Domain.ServiceEntries;

public static class ServiceEntryErrors
{
    public static readonly Error WorkStartedBeforeWorkOrder = Error.Validation("WorkStartedAt", "Work cannot start before the work order was started.");

    public static readonly Error WorkNotFinishedAfterStart = Error.Validation("WorkFinishedAt", "Work must finish after it starts.");

    public static readonly Error WorkFinishedInFuture = Error.Validation("WorkFinishedAt", "Work cannot finish in the future.");
}
