using System.Security.Claims;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Users.GetCurrentUser;

public static class GetCurrentUserEndpoint
{
    public static RouteGroupBuilder MapGetCurrentUser(this RouteGroupBuilder group)
    {
        group.MapGet("/me", async (ClaimsPrincipal user, GetCurrentUserHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(user, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("GetCurrentUser")
            .WithSummary("Get the current user account")
            .WithDescription("Returns the identifier, email, full name, role and active status of the account that owns the access token. Available to every signed-in user.")
            .RequireAuthorization()
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
