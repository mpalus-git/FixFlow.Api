using FixFlow.Api.Common.Concurrency;

namespace FixFlow.Api.Features.Devices.GetDevice;

public static class GetDeviceEndpoint
{
    public static RouteGroupBuilder MapGetDevice(this RouteGroupBuilder group)
    {
        group.MapGet("/{deviceId:guid}", async (Guid deviceId, GetDeviceHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(deviceId, cancellationToken);
                return result.ToOkWithETagOrProblem();
            })
            .WithName("GetDevice")
            .WithSummary("Get a device")
            .WithDescription("Returns a device by identifier, including archived devices so that historical work orders keep their device details.")
            .Produces<DeviceResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithETagResponse();

        return group;
    }
}
