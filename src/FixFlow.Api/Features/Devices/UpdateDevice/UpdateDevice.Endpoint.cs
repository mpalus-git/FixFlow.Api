using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Concurrency;

namespace FixFlow.Api.Features.Devices.UpdateDevice;

public static class UpdateDeviceEndpoint
{
    public static RouteGroupBuilder MapUpdateDevice(this RouteGroupBuilder group)
    {
        group.MapPut("/{deviceId:guid}", async (Guid deviceId, UpdateDeviceRequest request, UpdateDeviceHandler handler, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(deviceId, request, httpContext.IfMatch(), cancellationToken);
                return result.ToOkWithETagOrProblem();
            })
            .WithName("UpdateDevice")
            .WithSummary("Update a device")
            .WithDescription("Replaces the serial number, model, manufacturer and installation date of an active device. The owning client cannot be changed and archived devices cannot be modified. The If-Match header must carry the ETag of the device; a device changed in the meantime is rejected with 412. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<UpdateDeviceRequest>()
            .Produces<DeviceResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireIfMatch()
            .WithETagResponse();

        return group;
    }
}
