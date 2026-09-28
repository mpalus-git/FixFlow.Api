using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Pagination;

namespace FixFlow.Api.Features.Users.ListUsers;

public static class ListUsersEndpoint
{
    public static RouteGroupBuilder MapListUsers(this RouteGroupBuilder group)
    {
        group.MapGet("/", async ([AsParameters] ListUsersRequest request, ListUsersHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(request, cancellationToken)))
            .WithName("ListUsers")
            .WithSummary("List user accounts")
            .WithDescription("Returns one page of user accounts ordered by email, optionally limited to one role and to active or deactivated accounts, for example role=Technician&isActive=true when a dispatcher chooses a technician for a work order. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<ListUsersRequest>()
            .Produces<PagedResponse<UserResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }
}
