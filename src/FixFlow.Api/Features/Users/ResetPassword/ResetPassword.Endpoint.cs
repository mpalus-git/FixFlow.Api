using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Users.ResetPassword;

public static class ResetPasswordEndpoint
{
    public static RouteGroupBuilder MapResetPassword(this RouteGroupBuilder group)
    {
        group.MapPost("/{userId:guid}/password", async (Guid userId, ResetPasswordRequest request, ResetPasswordHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(userId, request, cancellationToken);
                return result.ToNoContentOrProblem();
            })
            .WithName("ResetPassword")
            .WithSummary("Reset the password of a user account")
            .WithDescription("Replaces the password of any account, for example when the user forgot it, lifts a lockout after failed sign-ins and revokes all refresh tokens of the account. Access tokens issued before stay valid until they expire. Available to administrators.")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithRequestValidation<ResetPasswordRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
