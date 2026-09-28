using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Devices.ArchiveDevice;

public static class ArchiveDeviceEndpoint
{
    public static RouteGroupBuilder MapArchiveDevice(this RouteGroupBuilder group)
    {
        group.MapPost("/{deviceId:guid}/archive", async (Guid deviceId, ArchiveDeviceHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(deviceId, cancellationToken);
                return result.ToNoContentOrProblem();
            })
            .WithName("ArchiveDevice")
            .WithSummary("Archive a device")
            .WithDescription("Hides the device from lists while keeping it and its history available by identifier. The serial number of an archived device stays reserved. Archiving an already archived device has no effect. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
