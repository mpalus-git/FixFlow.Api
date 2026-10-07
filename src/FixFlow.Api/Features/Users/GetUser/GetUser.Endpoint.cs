using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Users.GetUser;

public static class GetUserEndpoint
{
    public static RouteGroupBuilder MapGetUser(this RouteGroupBuilder group)
    {
        group.MapGet("/{userId:guid}", async (Guid userId, GetUserHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(userId, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("GetUser")
            .WithSummary("Get a user account")
            .WithDescription("Returns the identifier, email, full name, role and active status of any account, for example to fill the form for editing the account. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
