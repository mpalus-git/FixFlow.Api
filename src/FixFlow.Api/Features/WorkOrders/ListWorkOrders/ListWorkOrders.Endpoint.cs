using System.Security.Claims;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Pagination;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrders;

public static class ListWorkOrdersEndpoint
{
    public static RouteGroupBuilder MapListWorkOrders(this RouteGroupBuilder group)
    {
        group.MapGet("/", async ([AsParameters] ListWorkOrdersRequest request, ClaimsPrincipal user, ListWorkOrdersHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(request, user, cancellationToken)))
            .WithName("ListWorkOrders")
            .WithSummary("List work orders")
            .WithDescription("Returns one page of work orders ordered by due date, with the serial number and model of the device, the client name and the technician email, optionally filtered by status, technician, device, client, overdue flag and due date range, and searched by a fragment of the description, serial number or client name. Technicians only see work orders assigned to them.")
            .WithRequestValidation<ListWorkOrdersRequest>()
            .Produces<PagedResponse<WorkOrderListItemResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }
}
