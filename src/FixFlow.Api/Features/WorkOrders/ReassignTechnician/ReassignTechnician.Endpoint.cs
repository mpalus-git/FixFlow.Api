using System.Security.Claims;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Concurrency;

namespace FixFlow.Api.Features.WorkOrders.ReassignTechnician;

public static class ReassignTechnicianEndpoint
{
    public static RouteGroupBuilder MapReassignTechnician(this RouteGroupBuilder group)
    {
        group.MapPost("/{workOrderId:guid}/reassign", async (Guid workOrderId, ReassignTechnicianRequest request, ClaimsPrincipal user, ReassignTechnicianHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, request, user.GetUserId(), cancellationToken);
                return result.ToOkWithETagOrProblem();
            })
            .WithName("ReassignTechnician")
            .WithSummary("Move an assigned work order to another technician or date")
            .WithDescription("Changes the technician and optionally the deadline of a work order in the Assigned status in one operation, so the work order is never left unassigned in between. The same technician can be passed to change only the deadline. Work orders in progress cannot be reassigned. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<ReassignTechnicianRequest>()
            .Produces<WorkOrderResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithETagResponse();

        return group;
    }
}
