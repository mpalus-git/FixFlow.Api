using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Users.UpdateUser;

public static class UpdateUserEndpoint
{
    public static RouteGroupBuilder MapUpdateUser(this RouteGroupBuilder group)
    {
        group.MapPut("/{userId:guid}", async (Guid userId, UpdateUserRequest request, UpdateUserHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(userId, request, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("UpdateUser")
            .WithSummary("Update a user account")
            .WithDescription("Changes the first and last name of any account. The email, role and password are not changed. Available to administrators.")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithRequestValidation<UpdateUserRequest>()
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
