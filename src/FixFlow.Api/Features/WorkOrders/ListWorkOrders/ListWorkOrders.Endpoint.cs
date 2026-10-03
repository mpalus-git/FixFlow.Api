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
            .WithDescription("Returns one page of work orders sorted by the requested field and direction, by default by due date ascending, with ties ordered by identifier so that pages neither skip nor repeat work orders. Each work order comes with the serial number and model of the device, the client name and the technician email. The list can be filtered by status, technician, device, client, overdue flag and due date range, and searched by a fragment of the work order number, description, serial number or client name. Technicians only see work orders assigned to them.")
            .WithRequestValidation<ListWorkOrdersRequest>()
            .Produces<PagedResponse<WorkOrderListItemResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }
}
