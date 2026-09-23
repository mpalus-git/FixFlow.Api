using System.Globalization;
using Serilog;
using Serilog.Formatting.Compact;

namespace FixFlow.Api.Common.Logging;

public static class LoggingExtensions
{
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
}
