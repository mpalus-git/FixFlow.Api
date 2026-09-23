using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Clients.UpdateClient;

public static class UpdateClientEndpoint
{
    public static RouteGroupBuilder MapUpdateClient(this RouteGroupBuilder group)
    {
        group.MapPut("/{clientId:guid}", async (Guid clientId, UpdateClientRequest request, UpdateClientHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(clientId, request, cancellationToken);
                return result.Match<IResult>(client => TypedResults.Ok(client), errors => errors.ToProblem());
            })
            .WithName("UpdateClient")
            .WithSummary("Update a client")
            .WithDescription("Replaces the name, address and contact details of an active client. Archived clients cannot be modified. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<UpdateClientRequest>()
            .Produces<ClientResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
