using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Devices.CreateDevice;

public static class CreateDeviceEndpoint
{
    public static RouteGroupBuilder MapCreateDevice(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateDeviceRequest request, CreateDeviceHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.ToCreatedOrProblem(device => $"/api/v1/devices/{device.Id}");
            })
            .WithName("CreateDevice")
            .WithSummary("Create a device")
            .WithDescription("Registers a device of an active client. The serial number is trimmed, stored in upper case and must be unique, also among archived devices. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<CreateDeviceRequest>()
            .Produces<DeviceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
