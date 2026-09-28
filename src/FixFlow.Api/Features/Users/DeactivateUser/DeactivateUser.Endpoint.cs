using System.Security.Claims;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Users.DeactivateUser;

public static class DeactivateUserEndpoint
{
    public static RouteGroupBuilder MapDeactivateUser(this RouteGroupBuilder group)
    {
        group.MapPost("/{userId:guid}/deactivate", async (Guid userId, ClaimsPrincipal user, DeactivateUserHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(userId, user, cancellationToken);
                return result.ToNoContentOrProblem();
            })
            .WithName("DeactivateUser")
            .WithSummary("Deactivate a user account")
            .WithDescription("Blocks signing in to the account and revokes all its refresh tokens. Access tokens issued before stay valid until they expire. A technician with assigned or in-progress work orders cannot be deactivated until the work orders are unassigned or completed, and administrators cannot deactivate their own account. Deactivating an already deactivated account has no effect. Available to administrators.")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
