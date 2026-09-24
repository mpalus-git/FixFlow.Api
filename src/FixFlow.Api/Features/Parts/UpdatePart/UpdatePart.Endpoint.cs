using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Parts.UpdatePart;

public static class UpdatePartEndpoint
{
    public static RouteGroupBuilder MapUpdatePart(this RouteGroupBuilder group)
    {
        group.MapPut("/{partId:guid}", async (Guid partId, UpdatePartRequest request, UpdatePartHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(partId, request, cancellationToken);
                return result.Match<IResult>(part => TypedResults.Ok(part), errors => errors.ToProblem());
            })
            .WithName("UpdatePart")
            .WithSummary("Update a part")
            .WithDescription("Replaces the name, catalog number and unit price of an active part. The stock quantity is not changed and archived parts cannot be modified. A new price applies only to parts used afterwards. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<UpdatePartRequest>()
            .Produces<PartResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
