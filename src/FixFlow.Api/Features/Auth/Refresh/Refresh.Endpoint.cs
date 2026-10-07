using FixFlow.Api.Common.Auth;
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
                return result.ToOkOrProblem();
            })
            .WithName("RefreshTokens")
            .WithSummary("Exchange a refresh token for a new token pair")
            .WithDescription("Rotates the refresh token: the submitted token is revoked and a new access token and refresh token are returned. A refresh token that was already used can be exchanged again within a short grace period after its rotation (30 seconds by default) while its session is active, for example by a second browser tab or by a mobile client that lost the previous response; the earlier replacement stays valid. Submitting an already used refresh token after the grace period revokes every token of that session, which forces the user to log in again. A refresh token of a deactivated account is rejected and its session is revoked. Requests are rate limited per client IP address.")
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimiting.PolicyName)
            .WithRequestValidation<RefreshRequest>()
            .Produces<AuthTokensResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }
}
