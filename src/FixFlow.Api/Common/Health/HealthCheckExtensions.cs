using FixFlow.Api.Common.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace FixFlow.Api.Common.Health;

public static class HealthCheckExtensions
{
    private const string ReadinessTag = "ready";

    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<FixFlowDbContext>(tags: [ReadinessTag]);

        return services;
    }

    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadinessTag),
        });

        return app;
    }
}
