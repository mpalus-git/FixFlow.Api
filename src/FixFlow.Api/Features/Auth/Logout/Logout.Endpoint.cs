using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;

namespace FixFlow.Api.Features.Auth.Logout;

public static class LogoutEndpoint
{
    public static RouteGroupBuilder MapLogout(this RouteGroupBuilder group)
    {
        group.MapPost("/logout", async (LogoutRequest request, LogoutHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(request, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("Logout")
            .WithSummary("Log out and revoke the current session")
            .WithDescription("Revokes the submitted refresh token together with every token of its session. An access token is not required, so a client whose access token has already expired can still end its session. Access tokens issued before stay valid until they expire. The response is 204 also when the refresh token is unknown. Requests are rate limited per client IP address.")
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimiting.PolicyName)
            .WithRequestValidation<LogoutRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }
}
