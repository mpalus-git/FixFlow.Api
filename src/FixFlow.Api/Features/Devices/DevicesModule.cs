using FixFlow.Api.Features.Devices.CreateDevice;
using FluentValidation;

namespace FixFlow.Api.Features.Devices;

public static class DevicesModule
{
    public static IServiceCollection AddDevicesFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateDeviceHandler>();
        services.AddSingleton<IValidator<CreateDeviceRequest>, CreateDeviceRequestValidator>();

        return services;
    }

    public static IEndpointRouteBuilder MapDevicesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("Devices")
            .MapGroup("/api/v{version:apiVersion}/devices")
            .HasApiVersion(1)
            .WithTags("Devices")
            .RequireAuthorization();

        group.MapCreateDevice();

        return app;
    }
}
