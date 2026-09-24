using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.CreateWorkOrder;

public static class CreateWorkOrderEndpoint
{
    public static RouteGroupBuilder MapCreateWorkOrder(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateWorkOrderRequest request, CreateWorkOrderHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.Match<IResult>(
                    workOrder => TypedResults.Created($"/api/v1/work-orders/{workOrder.Id}", workOrder),
                    errors => errors.ToProblem());
            })
            .WithName("CreateWorkOrder")
            .WithSummary("Create a work order")
            .WithDescription("Creates an unassigned work order in the New status for an active device. The due date must be in the future. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<CreateWorkOrderRequest>()
            .Produces<WorkOrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
