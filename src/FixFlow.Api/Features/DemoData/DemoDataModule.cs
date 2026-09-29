using FixFlow.Api.Features.DemoData.ResetDemoData;

namespace FixFlow.Api.Features.DemoData;

public static class DemoDataModule
{
    public static IServiceCollection AddDemoDataFeatures(this IServiceCollection services)
    {
        services.AddScoped<ResetDemoDataHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapDemoDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("DemoData")
            .MapGroup("/api/v{version:apiVersion}/demo-data")
            .HasApiVersion(1)
            .WithTags("DemoData");

        group.MapResetDemoData();

        return app;
    }
}
