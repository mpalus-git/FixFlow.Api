using FixFlow.Api.Features.Clients.CreateClient;
using FixFlow.Api.Features.Clients.GetClient;
using FluentValidation;

namespace FixFlow.Api.Features.Clients;

public static class ClientsModule
{
    public static IServiceCollection AddClientsFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateClientHandler>();
        services.AddSingleton<IValidator<CreateClientRequest>, CreateClientRequestValidator>();
        services.AddScoped<GetClientHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapClientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("Clients")
            .MapGroup("/api/v{version:apiVersion}/clients")
            .HasApiVersion(1)
            .WithTags("Clients")
            .RequireAuthorization();

        group.MapCreateClient();
        group.MapGetClient();

        return app;
    }
}
