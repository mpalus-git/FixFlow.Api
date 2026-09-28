using ErrorOr;

namespace FixFlow.Api.Common.Concurrency;

public static class PreconditionErrors
{
    public const int FailedType = StatusCodes.Status412PreconditionFailed;
    public const int RequiredType = StatusCodes.Status428PreconditionRequired;

    public static readonly Error Failed = Error.Custom(
        FailedType,
        "Precondition.Failed",
        "The resource was changed since it was read. Get it again and repeat the change with the new ETag in If-Match.");

    public static readonly Error Required = Error.Custom(
        RequiredType,
        "Precondition.Required",
        "The If-Match header with the ETag of the resource is required.");
}
