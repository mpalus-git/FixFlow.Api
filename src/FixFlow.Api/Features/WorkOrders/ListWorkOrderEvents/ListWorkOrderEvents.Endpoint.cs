using System.Security.Claims;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrderEvents;

public static class ListWorkOrderEventsEndpoint
{
    public static RouteGroupBuilder MapListWorkOrderEvents(this RouteGroupBuilder group)
    {
        group.MapGet("/{workOrderId:guid}/events", async (Guid workOrderId, ClaimsPrincipal user, ListWorkOrderEventsHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, user, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("ListWorkOrderEvents")
            .WithSummary("List the history of a work order")
            .WithDescription("Returns the changes of a work order, oldest first: creation, edits, assignment, reassignment and unassignment of the technician, start, completion and invoicing, each with the time, the user who made the change and the technician and deadline after the change. Repeated start or completion requests do not add events. Technicians can only get the history of work orders assigned to them; other work orders are reported as not found.")
            .Produces<List<WorkOrderEventResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
