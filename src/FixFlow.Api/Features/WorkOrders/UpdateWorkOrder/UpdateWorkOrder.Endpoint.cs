using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;

public static class UpdateWorkOrderEndpoint
{
    public static RouteGroupBuilder MapUpdateWorkOrder(this RouteGroupBuilder group)
    {
        group.MapPut("/{workOrderId:guid}", async (Guid workOrderId, UpdateWorkOrderRequest request, UpdateWorkOrderHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, request, cancellationToken);
                return result.Match<IResult>(workOrder => TypedResults.Ok(workOrder), errors => errors.ToProblem());
            })
            .WithName("UpdateWorkOrder")
            .WithSummary("Update a work order")
            .WithDescription("Replaces the description, priority and due date of a work order without changing its status or technician. A changed due date must be in the future. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<UpdateWorkOrderRequest>()
            .Produces<WorkOrderResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
