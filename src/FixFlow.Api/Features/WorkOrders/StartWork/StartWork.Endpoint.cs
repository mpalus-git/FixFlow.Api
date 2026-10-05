using System.Security.Claims;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Concurrency;

namespace FixFlow.Api.Features.WorkOrders.StartWork;

public static class StartWorkEndpoint
{
    public static RouteGroupBuilder MapStartWork(this RouteGroupBuilder group)
    {
        group.MapPost("/{workOrderId:guid}/start", async (Guid workOrderId, StartWorkRequest? request, ClaimsPrincipal user, StartWorkHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, request, user, cancellationToken);
                return result.ToOkWithETagOrProblem();
            })
            .WithName("StartWork")
            .WithSummary("Start work on a work order")
            .WithDescription("Moves a work order assigned to the calling technician from Assigned to InProgress. A technician can have only one work order in progress at a time. The body is optional: without it the work starts at the server time, with startedAt the work starts at the time given by the client, for example recorded offline. Work orders of other technicians are reported as not found. Available to technicians only.")
            .RequireAuthorization(AuthorizationPolicies.TechnicianOnly)
            .Produces<WorkOrderResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithETagResponse();

        return group;
    }
}
