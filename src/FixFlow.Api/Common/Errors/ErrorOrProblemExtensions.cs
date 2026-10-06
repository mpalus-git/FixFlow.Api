using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Domain.Photos;

namespace FixFlow.Api.Common.Errors;

public static class ErrorOrProblemExtensions
{
    public const string ErrorCodeExtension = "errorCode";

    public static IResult ToProblem(this IReadOnlyList<Error> errors)
    {
        if (errors.Count > 0 && errors.All(error => error.Type == ErrorType.Validation))
        {
            return TypedResults.ValidationProblem(ValidationErrorKey.GroupByPropertyPath(
                errors,
                error => error.Code,
                error => error.Description));
        }

        var firstError = errors.Count > 0 ? errors[0] : Error.Unexpected();

        return TypedResults.Problem(
            detail: firstError.Description,
            statusCode: ToStatusCode(firstError.Type),
            extensions: new Dictionary<string, object?> { [ErrorCodeExtension] = firstError.Code });
    }

    private static int ToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ when (int)errorType is PreconditionErrors.FailedType
            or PreconditionErrors.RequiredType
            or PhotoErrors.ContentTooLargeType => (int)errorType,
        _ => StatusCodes.Status500InternalServerError,
    };
}
