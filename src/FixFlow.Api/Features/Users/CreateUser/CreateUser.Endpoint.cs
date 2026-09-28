using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Users.CreateUser;

public static class CreateUserEndpoint
{
    public static RouteGroupBuilder MapCreateUser(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateUserRequest request, CreateUserHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.ToCreatedOrProblem();
            })
            .WithName("CreateUser")
            .WithSummary("Create a user account")
            .WithDescription("Creates an account with the given role and initial password. Only administrators can create accounts; there is no public registration.")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithRequestValidation<CreateUserRequest>()
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
