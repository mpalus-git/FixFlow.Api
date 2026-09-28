using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Users.ActivateUser;

public static class ActivateUserEndpoint
{
    public static RouteGroupBuilder MapActivateUser(this RouteGroupBuilder group)
    {
        group.MapPost("/{userId:guid}/activate", async (Guid userId, ActivateUserHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(userId, cancellationToken);
                return result.ToNoContentOrProblem();
            })
            .WithName("ActivateUser")
            .WithSummary("Activate a user account")
            .WithDescription("Allows a deactivated account to sign in again with its current password. Activating an active account has no effect. Available to administrators.")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
