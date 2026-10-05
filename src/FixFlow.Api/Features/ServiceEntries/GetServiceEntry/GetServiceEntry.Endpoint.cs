using System.Security.Claims;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.ServiceEntries.GetServiceEntry;

public static class GetServiceEntryEndpoint
{
    public static RouteGroupBuilder MapGetServiceEntry(this RouteGroupBuilder group)
    {
        group.MapGet("/{serviceEntryId:guid}", async (Guid workOrderId, Guid serviceEntryId, ClaimsPrincipal user, GetServiceEntryHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, serviceEntryId, user, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("GetServiceEntry")
            .WithSummary("Get a service entry")
            .WithDescription("Returns one work or correction entry of a work order. Technicians see only entries of work orders assigned to them; other work orders are reported as not found. An entry of a different work order is reported as not found.")
            .Produces<ServiceEntryResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
