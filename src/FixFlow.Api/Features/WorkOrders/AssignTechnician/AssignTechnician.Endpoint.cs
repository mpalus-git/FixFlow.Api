using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.AssignTechnician;

public static class AssignTechnicianEndpoint
{
    public static RouteGroupBuilder MapAssignTechnician(this RouteGroupBuilder group)
    {
        group.MapPost("/{workOrderId:guid}/assign", async (Guid workOrderId, AssignTechnicianRequest request, AssignTechnicianHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, request, cancellationToken);
                return result.Match<IResult>(workOrder => TypedResults.Ok(workOrder), errors => errors.ToProblem());
            })
            .WithName("AssignTechnician")
            .WithSummary("Assign a technician to a work order")
            .WithDescription("Assigns a user with the Technician role to a work order in the New status and moves it to Assigned. To change the technician, unassign the current one first. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<AssignTechnicianRequest>()
            .Produces<WorkOrderResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
