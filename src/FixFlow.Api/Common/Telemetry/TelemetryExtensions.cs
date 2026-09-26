using FixFlow.Api.Common.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FixFlow.Api.Common.Telemetry;

public static class TelemetryExtensions
{
    public const string ConsoleExporterEnabledSettingKey = "OpenTelemetry:ConsoleExporterEnabled";
    public const string ServiceName = "FixFlow.Api";

    public static IServiceCollection AddApplicationTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        if (BuildTimeOpenApiGeneration.IsRunning || !configuration.GetValue<bool>(ConsoleExporterEnabledSettingKey))
        {
            return services;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddSource("System.Net.Http", "Npgsql")
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter("System.Net.Http", "System.Runtime")
                .AddConsoleExporter());

        return services;
    }
}
