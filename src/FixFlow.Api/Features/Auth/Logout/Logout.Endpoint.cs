using System.Security.Claims;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;

namespace FixFlow.Api.Features.Auth.Logout;

public static class LogoutEndpoint
{
    public static RouteGroupBuilder MapLogout(this RouteGroupBuilder group)
    {
        group.MapPost("/logout", async (LogoutRequest request, ClaimsPrincipal user, LogoutHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(user.GetUserId(), request, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("Logout")
            .WithSummary("Log out and revoke the current session")
            .WithDescription("Revokes the submitted refresh token together with every token of its session. The access token stays valid until it expires. The response is 204 also when the refresh token is unknown or belongs to another user.")
            .RequireAuthorization()
            .WithRequestValidation<LogoutRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }
}
