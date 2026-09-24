using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.UnassignTechnician;

public static class UnassignTechnicianEndpoint
{
    public static RouteGroupBuilder MapUnassignTechnician(this RouteGroupBuilder group)
    {
        group.MapPost("/{workOrderId:guid}/unassign", async (Guid workOrderId, UnassignTechnicianHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, cancellationToken);
                return result.Match<IResult>(workOrder => TypedResults.Ok(workOrder), errors => errors.ToProblem());
            })
            .WithName("UnassignTechnician")
            .WithSummary("Unassign the technician from a work order")
            .WithDescription("Removes the technician from a work order in the Assigned status and moves it back to New. Work that has already started cannot be unassigned. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .Produces<WorkOrderResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
