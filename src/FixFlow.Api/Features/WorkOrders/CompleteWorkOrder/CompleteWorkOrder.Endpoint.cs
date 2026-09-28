using System.Security.Claims;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.CompleteWorkOrder;

public static class CompleteWorkOrderEndpoint
{
    public static RouteGroupBuilder MapCompleteWorkOrder(this RouteGroupBuilder group)
    {
        group.MapPost("/{workOrderId:guid}/complete", async (Guid workOrderId, ClaimsPrincipal user, CompleteWorkOrderHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, user, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("CompleteWorkOrder")
            .WithSummary("Complete a work order")
            .WithDescription("Moves a work order from InProgress to Completed. The work order must have at least one service entry. After completion no service entries can be added and the work order cannot be edited. Available to the assigned technician and, as a fallback, to dispatchers and administrators; work orders of other technicians are reported as not found.")
            .Produces<WorkOrderResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
