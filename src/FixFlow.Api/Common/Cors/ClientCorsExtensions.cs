using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace FixFlow.Api.Common.Cors;

public sealed class ClientCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}

public static class ClientCorsExtensions
{
    public static IServiceCollection AddClientCors(this IServiceCollection services)
    {
        services.AddOptions<ClientCorsOptions>().BindConfiguration(ClientCorsOptions.SectionName);
        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<ClientCorsOptions>>((corsOptions, clientCorsOptions) =>
                corsOptions.AddDefaultPolicy(policy => policy
                    .WithOrigins(clientCorsOptions.Value.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders(HeaderNames.RetryAfter, HeaderNames.ETag)));

        return services;
    }
}
