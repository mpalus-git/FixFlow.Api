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
                return result.Match<IResult>(
                    added => added.WasAlreadyAdded
                        ? TypedResults.Ok(added.Entry)
                        : TypedResults.Created($"/api/v1/work-orders/{added.Entry.WorkOrderId}/service-entries/{added.Entry.Id}", added.Entry),
                    errors => errors.ToProblem());
            })
            .WithName("AddServiceEntry")
            .WithSummary("Add a service entry")
            .WithDescription("Adds a work entry or a correction entry to a work order in progress assigned to the calling technician. Parts used in a work entry are taken from stock at their current price and the entry is rejected if the stock would drop below zero. A correction returns parts to stock, at most the net quantity used on the work order, at the price of their last use. Entries cannot be changed or deleted. Work orders of other technicians are reported as not found. Available to technicians only. When the request carries an identifier of an entry already added by the same technician to the same work order, the stored entry is returned with status 200 without taking parts from stock again and without checking the work order status, so a queued entry can be safely sent again even after the work order was completed; the request content is not compared. An identifier used on another work order or by another technician is rejected with a conflict.")
            .RequireAuthorization(AuthorizationPolicies.TechnicianOnly)
            .WithRequestValidation<AddServiceEntryRequest>()
            .Produces<ServiceEntryResponse>()
            .Produces<ServiceEntryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
