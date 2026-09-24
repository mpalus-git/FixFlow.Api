using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Parts.ArchivePart;

public static class ArchivePartEndpoint
{
    public static RouteGroupBuilder MapArchivePart(this RouteGroupBuilder group)
    {
        group.MapPost("/{partId:guid}/archive", async (Guid partId, ArchivePartHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(partId, cancellationToken);
                return result.Match<IResult>(_ => TypedResults.NoContent(), errors => errors.ToProblem());
            })
            .WithName("ArchivePart")
            .WithSummary("Archive a part")
            .WithDescription("Hides the part from lists and prevents it from being used in new service entries, while keeping it available by identifier. The catalog number of an archived part stays reserved and its stock can still be replenished. Archiving an already archived part has no effect. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
