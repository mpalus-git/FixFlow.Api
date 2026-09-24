using System.Security.Claims;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.ServiceEntries.AddServiceEntry;

public static class AddServiceEntryEndpoint
{
    public static RouteGroupBuilder MapAddServiceEntry(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (Guid workOrderId, AddServiceEntryRequest request, ClaimsPrincipal user, AddServiceEntryHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, request, user, cancellationToken);
                return result.Match<IResult>(entry => TypedResults.Created((string?)null, entry), errors => errors.ToProblem());
            })
            .WithName("AddServiceEntry")
            .WithSummary("Add a service entry")
            .WithDescription("Adds a work entry or a correction entry to a work order in progress assigned to the calling technician. Parts used in a work entry are taken from stock at their current price and the entry is rejected if the stock would drop below zero. A correction returns parts to stock, at most the net quantity used on the work order, at the price of their last use. Entries cannot be changed or deleted. Work orders of other technicians are reported as not found. Available to technicians only.")
            .RequireAuthorization(AuthorizationPolicies.TechnicianOnly)
            .WithRequestValidation<AddServiceEntryRequest>()
            .Produces<ServiceEntryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
