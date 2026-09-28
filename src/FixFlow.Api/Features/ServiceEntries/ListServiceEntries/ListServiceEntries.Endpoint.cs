using System.Security.Claims;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.ServiceEntries.ListServiceEntries;

public static class ListServiceEntriesEndpoint
{
    public static RouteGroupBuilder MapListServiceEntries(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (Guid workOrderId, ClaimsPrincipal user, ListServiceEntriesHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, user, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("ListServiceEntries")
            .WithSummary("List service entries of a work order")
            .WithDescription("Returns all work and correction entries of a work order, oldest first. Technicians see only entries of work orders assigned to them; other work orders are reported as not found.")
            .Produces<List<ServiceEntryResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
