using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Pagination;

namespace FixFlow.Api.Features.Clients.ListClients;

public static class ListClientsEndpoint
{
    public static RouteGroupBuilder MapListClients(this RouteGroupBuilder group)
    {
        group.MapGet("/", async ([AsParameters] ListClientsRequest request, ListClientsHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(request, cancellationToken)))
            .WithName("ListClients")
            .WithSummary("List active clients")
            .WithDescription("Returns one page of active clients ordered by name. Archived clients are not listed. The optional search matches a fragment of the client name regardless of letter case.")
            .WithRequestValidation<ListClientsRequest>()
            .Produces<PagedResponse<ClientResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }
}
