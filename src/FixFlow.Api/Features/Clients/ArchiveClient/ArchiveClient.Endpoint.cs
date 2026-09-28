using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Clients.ArchiveClient;

public static class ArchiveClientEndpoint
{
    public static RouteGroupBuilder MapArchiveClient(this RouteGroupBuilder group)
    {
        group.MapPost("/{clientId:guid}/archive", async (Guid clientId, ArchiveClientHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(clientId, cancellationToken);
                return result.ToNoContentOrProblem();
            })
            .WithName("ArchiveClient")
            .WithSummary("Archive a client")
            .WithDescription("Hides the client from lists while keeping it and its history available by identifier. All active devices of the client are archived in the same operation and no new devices can be added to it. Archiving an already archived client has no effect. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
