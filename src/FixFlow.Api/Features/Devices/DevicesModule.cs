using FixFlow.Api.Features.Devices.CreateDevice;
using FixFlow.Api.Features.Devices.GetDevice;
using FixFlow.Api.Features.Devices.ListDevices;
using FixFlow.Api.Features.Devices.UpdateDevice;
using FluentValidation;

namespace FixFlow.Api.Features.Devices;

public static class DevicesModule
{
    public static IServiceCollection AddDevicesFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateDeviceHandler>();
        services.AddSingleton<IValidator<CreateDeviceRequest>, CreateDeviceRequestValidator>();
        services.AddScoped<GetDeviceHandler>();
        services.AddScoped<ListDevicesHandler>();
        services.AddSingleton<IValidator<ListDevicesRequest>, ListDevicesRequestValidator>();
        services.AddScoped<UpdateDeviceHandler>();
        services.AddSingleton<IValidator<UpdateDeviceRequest>, UpdateDeviceRequestValidator>();

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
        group.MapListDevices();
        group.MapUpdateDevice();

        return app;
    }
}
