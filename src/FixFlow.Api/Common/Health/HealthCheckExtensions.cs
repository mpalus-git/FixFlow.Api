using FixFlow.Api.Common.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace FixFlow.Api.Common.Health;

public static class HealthCheckExtensions
{
    public const string LivenessPath = "/health";
    public const string ReadinessPath = "/health/ready";

    private const string ReadinessTag = "ready";

    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<FixFlowDbContext>(tags: [ReadinessTag]);

        return services;
    }

    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(LivenessPath, new HealthCheckOptions
        {
            Predicate = _ => false,
        });
        app.MapHealthChecks(ReadinessPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadinessTag),
        });

        return app;
    }
}
