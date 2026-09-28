using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Concurrency;

namespace FixFlow.Api.Features.Clients.UpdateClient;

public static class UpdateClientEndpoint
{
    public static RouteGroupBuilder MapUpdateClient(this RouteGroupBuilder group)
    {
        group.MapPut("/{clientId:guid}", async (Guid clientId, UpdateClientRequest request, UpdateClientHandler handler, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(clientId, request, httpContext.IfMatch(), cancellationToken);
                return result.ToOkWithETagOrProblem();
            })
            .WithName("UpdateClient")
            .WithSummary("Update a client")
            .WithDescription("Replaces the name, address and contact details of an active client. Archived clients cannot be modified. The If-Match header must carry the ETag of the client; a client changed in the meantime is rejected with 412. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<UpdateClientRequest>()
            .Produces<ClientResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireIfMatch()
            .WithETagResponse();

        return group;
    }
}
