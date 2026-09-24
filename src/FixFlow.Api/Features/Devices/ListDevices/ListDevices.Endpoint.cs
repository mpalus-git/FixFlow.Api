using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Pagination;

namespace FixFlow.Api.Features.Devices.ListDevices;

public static class ListDevicesEndpoint
{
    public static RouteGroupBuilder MapListDevices(this RouteGroupBuilder group)
    {
        group.MapGet("/", async ([AsParameters] ListDevicesRequest request, ListDevicesHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(request, cancellationToken)))
            .WithName("ListDevices")
            .WithSummary("List active devices")
            .WithDescription("Returns one page of active devices ordered by serial number. Archived devices are not listed. The optional client filter limits the list to devices of one client, and the optional search matches a fragment of the serial number, model or manufacturer regardless of letter case.")
            .WithRequestValidation<ListDevicesRequest>()
            .Produces<PagedResponse<DeviceResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }
}
