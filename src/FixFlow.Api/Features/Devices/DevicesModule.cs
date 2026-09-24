using FixFlow.Api.Features.Devices.CreateDevice;
using FixFlow.Api.Features.Devices.GetDevice;
using FluentValidation;

namespace FixFlow.Api.Features.Devices;

public static class DevicesModule
{
    public static IServiceCollection AddDevicesFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateDeviceHandler>();
        services.AddSingleton<IValidator<CreateDeviceRequest>, CreateDeviceRequestValidator>();
        services.AddScoped<GetDeviceHandler>();

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
        group.MapGetDevice();

        return app;
    }
}
