using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Concurrency;

namespace FixFlow.Api.Features.Parts.UpdatePart;

public static class UpdatePartEndpoint
{
    public static RouteGroupBuilder MapUpdatePart(this RouteGroupBuilder group)
    {
        group.MapPut("/{partId:guid}", async (Guid partId, UpdatePartRequest request, UpdatePartHandler handler, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(partId, request, httpContext.IfMatch(), cancellationToken);
                return result.ToOkWithETagOrProblem();
            })
            .WithName("UpdatePart")
            .WithSummary("Update a part")
            .WithDescription("Replaces the name, catalog number and unit price of an active part. The stock quantity is not changed and archived parts cannot be modified. A new price applies only to parts used afterwards. The If-Match header must carry the ETag of the part; a part changed in the meantime is rejected with 412. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<UpdatePartRequest>()
            .Produces<PartResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireIfMatch()
            .WithETagResponse();

        return group;
    }
}
