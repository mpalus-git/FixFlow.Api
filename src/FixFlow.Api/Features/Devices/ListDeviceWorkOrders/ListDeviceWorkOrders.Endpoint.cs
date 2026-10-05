using System.Security.Claims;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;
using FixFlow.Api.Common.Pagination;

namespace FixFlow.Api.Features.Devices.ListDeviceWorkOrders;

public static class ListDeviceWorkOrdersEndpoint
{
    public static RouteGroupBuilder MapListDeviceWorkOrders(this RouteGroupBuilder group)
    {
        group.MapGet("/{deviceId:guid}/work-orders", async (Guid deviceId, [AsParameters] ListDeviceWorkOrdersRequest request, ClaimsPrincipal user, ListDeviceWorkOrdersHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(deviceId, request, user, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("ListDeviceWorkOrders")
            .WithSummary("List the service history of a device")
            .WithDescription("Returns one page of work orders of a device, newest first, with ties ordered by identifier. Each work order comes with the name of the assigned technician and the number of its service entries. Dispatchers and administrators see all work orders of the device, including archived devices. Technicians see only work orders assigned to them; a device without such work orders is reported as not found.")
            .WithRequestValidation<ListDeviceWorkOrdersRequest>()
            .Produces<PagedResponse<DeviceWorkOrderHistoryItemResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
