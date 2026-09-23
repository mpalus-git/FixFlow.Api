using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Auth.Refresh;

public static class RefreshEndpoint
{
    public static RouteGroupBuilder MapRefresh(this RouteGroupBuilder group)
    {
        group.MapPost("/refresh", async (RefreshRequest request, RefreshHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.Match<IResult>(tokens => TypedResults.Ok(tokens), errors => errors.ToProblem());
            })
            .WithName("RefreshTokens")
            .WithSummary("Exchange a refresh token for a new token pair")
            .WithDescription("Rotates the refresh token: the submitted token is revoked and a new access token and refresh token are returned. Submitting a refresh token that was already used revokes every token of that session, which forces the user to log in again.")
            .AllowAnonymous()
            .WithRequestValidation<RefreshRequest>()
            .Produces<AuthTokensResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }
}
