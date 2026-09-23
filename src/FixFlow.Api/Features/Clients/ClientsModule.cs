using FixFlow.Api.Features.Clients.ArchiveClient;
using FixFlow.Api.Features.Clients.CreateClient;
using FixFlow.Api.Features.Clients.GetClient;
using FixFlow.Api.Features.Clients.ListClients;
using FixFlow.Api.Features.Clients.UpdateClient;
using FluentValidation;

namespace FixFlow.Api.Features.Clients;

public static class ClientsModule
{
    public static IServiceCollection AddClientsFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateClientHandler>();
        services.AddSingleton<IValidator<CreateClientRequest>, CreateClientRequestValidator>();
        services.AddScoped<GetClientHandler>();
        services.AddScoped<ListClientsHandler>();
        services.AddSingleton<IValidator<ListClientsRequest>, ListClientsRequestValidator>();
        services.AddScoped<UpdateClientHandler>();
        services.AddSingleton<IValidator<UpdateClientRequest>, UpdateClientRequestValidator>();
        services.AddScoped<ArchiveClientHandler>();

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
        group.MapListClients();
        group.MapUpdateClient();
        group.MapArchiveClient();

        return app;
    }
}
