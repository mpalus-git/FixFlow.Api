using System.Security.Claims;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.GetWorkOrder;

public static class GetWorkOrderEndpoint
{
    public static RouteGroupBuilder MapGetWorkOrder(this RouteGroupBuilder group)
    {
        group.MapGet("/{workOrderId:guid}", async (Guid workOrderId, ClaimsPrincipal user, GetWorkOrderHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, user, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("GetWorkOrder")
            .WithSummary("Get a work order")
            .WithDescription("Returns a work order by identifier. Technicians can only get work orders assigned to them; other work orders are reported as not found.")
            .Produces<WorkOrderResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
