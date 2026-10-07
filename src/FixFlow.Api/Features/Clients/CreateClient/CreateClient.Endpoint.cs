using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Concurrency;

namespace FixFlow.Api.Features.Clients.CreateClient;

public static class CreateClientEndpoint
{
    public static RouteGroupBuilder MapCreateClient(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateClientRequest request, CreateClientHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.ToCreatedWithETagOrProblem(client => $"/api/v1/clients/{client.Id}");
            })
            .WithName("CreateClient")
            .WithSummary("Create a client")
            .WithDescription("Creates a client with a postal address and contact details. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<CreateClientRequest>()
            .WithETagResponse()
            .Produces<ClientResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }
}
