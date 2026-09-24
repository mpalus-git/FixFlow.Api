using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Pagination;

namespace FixFlow.Api.Features.Parts.ListParts;

public static class ListPartsEndpoint
{
    public static RouteGroupBuilder MapListParts(this RouteGroupBuilder group)
    {
        group.MapGet("/", async ([AsParameters] ListPartsRequest request, ListPartsHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(request, cancellationToken)))
            .WithName("ListParts")
            .WithSummary("List active parts")
            .WithDescription("Returns one page of active parts with their current stock quantities, ordered by name and catalog number. Archived parts are not listed. The optional search matches a fragment of the name or catalog number regardless of letter case.")
            .WithRequestValidation<ListPartsRequest>()
            .Produces<PagedResponse<PartResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }
}
