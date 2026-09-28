using System.Security.Claims;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.StartWork;

public static class StartWorkEndpoint
{
    public static RouteGroupBuilder MapStartWork(this RouteGroupBuilder group)
    {
        group.MapPost("/{workOrderId:guid}/start", async (Guid workOrderId, ClaimsPrincipal user, StartWorkHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, user, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("StartWork")
            .WithSummary("Start work on a work order")
            .WithDescription("Moves a work order assigned to the calling technician from Assigned to InProgress. A technician can have only one work order in progress at a time. Work orders of other technicians are reported as not found. Available to technicians only.")
            .RequireAuthorization(AuthorizationPolicies.TechnicianOnly)
            .Produces<WorkOrderResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
