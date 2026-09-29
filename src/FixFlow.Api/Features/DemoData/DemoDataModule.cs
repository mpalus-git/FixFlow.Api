using FixFlow.Api.Features.DemoData.ResetDemoData;

namespace FixFlow.Api.Features.DemoData;

public static class DemoDataModule
{
    public static IServiceCollection AddDemoDataFeatures(this IServiceCollection services)
    {
        services.AddScoped<ResetDemoDataHandler>();

        return services;
    }
}
