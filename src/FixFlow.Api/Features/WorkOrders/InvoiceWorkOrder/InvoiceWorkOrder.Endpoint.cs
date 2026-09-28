using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.InvoiceWorkOrder;

public static class InvoiceWorkOrderEndpoint
{
    public static RouteGroupBuilder MapInvoiceWorkOrder(this RouteGroupBuilder group)
    {
        group.MapPost("/{workOrderId:guid}/invoice", async (Guid workOrderId, InvoiceWorkOrderHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("InvoiceWorkOrder")
            .WithSummary("Mark a work order as invoiced")
            .WithDescription("Moves a completed work order to Invoiced, which is the final status. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .Produces<WorkOrderResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
