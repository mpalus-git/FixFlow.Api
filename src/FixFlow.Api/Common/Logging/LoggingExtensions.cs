using System.Globalization;
using FixFlow.Api.Common.Health;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace FixFlow.Api.Common.Logging;

public static class LoggingExtensions
{
    public const string ClientIpProperty = "ClientIp";

    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        var runsInContainer = builder.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER");
        var writesToFile = builder.Environment.IsDevelopment() && !runsInContainer;

        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();

            if (runsInContainer)
            {
                configuration.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
            else
            {
                configuration.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
            }

            if (writesToFile)
            {
                configuration.WriteTo.File(
                    "logs/fixflow-.log",
                    formatProvider: CultureInfo.InvariantCulture,
                    rollingInterval: RollingInterval.Day);
            }
        });

        return builder;
    }

    public static WebApplication UseRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = EnrichWithClientIp;
            options.GetLevel = GetRequestLogLevel;
        });
        return app;
    }

    public static LogEventLevel GetRequestLogLevel(HttpContext httpContext, double elapsedMilliseconds, Exception? exception)
    {
        if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        return httpContext.Request.Path.StartsWithSegments(HealthCheckExtensions.LivenessPath, StringComparison.OrdinalIgnoreCase)
            || httpContext.Request.Path.StartsWithSegments(HealthCheckExtensions.SystemReadinessPath, StringComparison.OrdinalIgnoreCase)
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
    }

    public static void EnrichWithClientIp(IDiagnosticContext diagnosticContext, HttpContext httpContext) =>
        diagnosticContext.Set(ClientIpProperty, httpContext.Connection.RemoteIpAddress?.ToString());
}
