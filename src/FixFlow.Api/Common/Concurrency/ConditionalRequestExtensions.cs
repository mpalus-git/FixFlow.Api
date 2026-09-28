using ErrorOr;
using FixFlow.Api.Common.Errors;
using Microsoft.Net.Http.Headers;

namespace FixFlow.Api.Common.Concurrency;

public sealed record IfMatchRequiredMetadata;

public sealed record ETagResponseMetadata;

public static class ConditionalRequestExtensions
{
    public static RouteHandlerBuilder RequireIfMatch(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter(async (context, next) =>
                string.IsNullOrWhiteSpace(context.HttpContext.Request.Headers.IfMatch)
                    ? new[] { PreconditionErrors.Required }.ToProblem()
                    : await next(context))
            .WithMetadata(new IfMatchRequiredMetadata())
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired);

    public static RouteHandlerBuilder WithETagResponse(this RouteHandlerBuilder builder) =>
        builder.WithMetadata(new ETagResponseMetadata());

    public static string IfMatch(this HttpContext httpContext) => httpContext.Request.Headers.IfMatch.ToString();

    public static IResult WithETag(this IResult result, uint version) => new ETagResult(result, EntityTag.Format(version));

    public static IResult ToOkWithETagOrProblem<TValue>(this ErrorOr<Versioned<TValue>> result) =>
        result.Match(versioned => TypedResults.Ok(versioned.Value).WithETag(versioned.Version), errors => errors.ToProblem());

    public static IResult ToCreatedWithETagOrProblem<TValue>(this ErrorOr<Versioned<TValue>> result, Func<TValue, string> location) =>
        result.Match(
            versioned => TypedResults.Created(location(versioned.Value), versioned.Value).WithETag(versioned.Version),
            errors => errors.ToProblem());

    private sealed class ETagResult(IResult inner, string entityTag) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers[HeaderNames.ETag] = entityTag;
            return inner.ExecuteAsync(httpContext);
        }
    }
}
