using System.Security.Claims;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.WorkOrders.GetServiceProtocol;

public static class GetServiceProtocolEndpoint
{
    public const string PdfContentType = "application/pdf";

    public static RouteGroupBuilder MapGetServiceProtocol(this RouteGroupBuilder group)
    {
        group.MapGet("/{workOrderId:guid}/protocol", async (Guid workOrderId, ClaimsPrincipal user, GetServiceProtocolHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(workOrderId, user, cancellationToken);
                return result.Match<IResult>(
                    protocol => TypedResults.File(protocol.Content, PdfContentType, protocol.FileName),
                    errors => errors.ToProblem());
            })
            .WithName("GetServiceProtocol")
            .WithSummary("Download the service protocol")
            .WithDescription("Returns the service protocol of a completed or invoiced work order as a PDF document in Polish, named after the work order number, for example protokol-ZL-2026-0042.pdf. The protocol lists the client, the device, all service entries with work time, the net used parts with amounts in PLN and fields for signatures. Technicians can only download protocols of work orders assigned to them; other work orders are reported as not found.")
            .Produces<Stream>(StatusCodes.Status200OK, PdfContentType)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
