using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Parts.CreatePart;

public static class CreatePartEndpoint
{
    public static RouteGroupBuilder MapCreatePart(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreatePartRequest request, CreatePartHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.Match<IResult>(
                    part => TypedResults.Created($"/api/v1/parts/{part.Id}", part),
                    errors => errors.ToProblem());
            })
            .WithName("CreatePart")
            .WithSummary("Create a part")
            .WithDescription("Adds a part to the warehouse catalog with an initial stock quantity. The catalog number is trimmed, stored in upper case and must be unique, also among archived parts. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .WithRequestValidation<CreatePartRequest>()
            .Produces<PartResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
