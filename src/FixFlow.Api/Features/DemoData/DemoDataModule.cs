using FixFlow.Api.Common.OpenApi;
using FixFlow.Api.Common.Persistence.Seeding;
using FixFlow.Api.Features.DemoData.ResetDemoData;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Features.DemoData;

public static class DemoDataModule
{
    public static IServiceCollection AddDemoDataFeatures(this IServiceCollection services)
    {
        services.AddOptions<DemoDataOptions>()
            .BindConfiguration(DemoDataOptions.SectionName)
            .Validate<IOptions<DemoUsersOptions>>(
                (options, demoUsersOptions) => !options.Enabled || demoUsersOptions.Value.Enabled,
                "Demo users seeding must be enabled when demo data seeding is enabled.")
            .ValidateOnStartOutsideBuildTimeGeneration();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<ResetDemoDataHandler>();

        return services;
    }

    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        if (scope.ServiceProvider.GetRequiredService<IOptions<DemoDataOptions>>().Value.Enabled)
        {
            var demoDataSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            await demoDataSeeder.SeedIfDatabaseIsEmptyAsync(app.Lifetime.ApplicationStopping);
        }
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
