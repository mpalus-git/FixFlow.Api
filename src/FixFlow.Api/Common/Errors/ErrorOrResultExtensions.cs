using ErrorOr;

namespace FixFlow.Api.Common.Errors;

public static class ErrorOrResultExtensions
{
    public static IResult ToOkOrProblem<TValue>(this ErrorOr<TValue> result) =>
        result.Match<IResult>(value => TypedResults.Ok(value), errors => errors.ToProblem());

    public static IResult ToNoContentOrProblem<TValue>(this ErrorOr<TValue> result) =>
        result.Match<IResult>(_ => TypedResults.NoContent(), errors => errors.ToProblem());

    public static IResult ToCreatedOrProblem<TValue>(this ErrorOr<TValue> result, Func<TValue, string>? location = null) =>
        result.Match<IResult>(value => TypedResults.Created(location?.Invoke(value), value), errors => errors.ToProblem());
}
