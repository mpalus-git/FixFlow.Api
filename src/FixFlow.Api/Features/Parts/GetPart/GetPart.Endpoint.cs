using FixFlow.Api.Common.Concurrency;

namespace FixFlow.Api.Features.Parts.GetPart;

public static class GetPartEndpoint
{
    public static RouteGroupBuilder MapGetPart(this RouteGroupBuilder group)
    {
        group.MapGet("/{partId:guid}", async (Guid partId, GetPartHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(partId, cancellationToken);
                return result.ToOkWithETagOrProblem();
            })
            .WithName("GetPart")
            .WithSummary("Get a part")
            .WithDescription("Returns a part with its current stock quantity by identifier, including archived parts so that historical service entries keep their part details.")
            .Produces<PartResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithETagResponse();

        return group;
    }
}
