using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Clients.GetClient;

public static class GetClientEndpoint
{
    public static RouteGroupBuilder MapGetClient(this RouteGroupBuilder group)
    {
        group.MapGet("/{clientId:guid}", async (Guid clientId, GetClientHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(clientId, cancellationToken);
                return result.Match<IResult>(client => TypedResults.Ok(client), errors => errors.ToProblem());
            })
            .WithName("GetClient")
            .WithSummary("Get a client")
            .WithDescription("Returns a client by identifier, including archived clients so that historical work orders keep their client details.")
            .Produces<ClientResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
