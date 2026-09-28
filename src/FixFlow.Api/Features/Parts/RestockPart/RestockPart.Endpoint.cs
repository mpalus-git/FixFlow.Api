using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Parts.RestockPart;

public static class RestockPartEndpoint
{
    public static RouteGroupBuilder MapRestockPart(this RouteGroupBuilder group)
    {
        group.MapPost("/{partId:guid}/restock", async (Guid partId, RestockPartRequest request, RestockPartHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(partId, request, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("RestockPart")
            .WithSummary("Register a part delivery")
            .WithDescription("Increases the stock quantity of a part by the number of delivered units, also for archived parts. A delivery registered at the same time as another stock change is rejected with a conflict and can be retried. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<RestockPartRequest>()
            .Produces<PartResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
