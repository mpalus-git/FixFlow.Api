using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace FixFlow.Api.Features.WorkOrders.GetServiceProtocol;

public sealed class GetServiceProtocolHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<ServiceProtocolFile>> HandleAsync(Guid workOrderId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders
            .AsNoTracking()
            .VisibleTo(user)
            .SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var availability = workOrder.EnsureCanIssueServiceProtocol();
        if (availability.IsError)
        {
            return availability.Errors;
        }

        var device = await dbContext.Devices.AsNoTracking().SingleAsync(device => device.Id == workOrder.DeviceId, cancellationToken);
        var client = await dbContext.Clients.AsNoTracking().SingleAsync(client => client.Id == device.ClientId, cancellationToken);
        var technicianName = await dbContext.Users
            .Where(user => user.Id == workOrder.TechnicianId)
            .Select(user => user.FullName)
            .SingleOrDefaultAsync(cancellationToken);
        var serviceEntries = await dbContext.ServiceEntries
            .AsNoTracking()
            .Where(entry => entry.WorkOrderId == workOrderId)
            .OrderBy(entry => entry.CreatedAt)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);

        var protocol = new ServiceProtocol(
            workOrder,
            device,
            client,
            technicianName,
            serviceEntries,
            await DescribeUsedPartsAsync(serviceEntries, cancellationToken),
            await LoadClientSignatureAsync(workOrder.ClientSignaturePhotoId, cancellationToken),
            timeProvider.GetUtcNow());

        return ServiceProtocolFile.For(workOrder, new ServiceProtocolDocument(protocol).GeneratePdf());
    }

    private async Task<byte[]?> LoadClientSignatureAsync(Guid? photoId, CancellationToken cancellationToken) =>
        photoId is null
            ? null
            : await dbContext.Photos
                .Where(photo => photo.Id == photoId)
                .Select(photo => photo.Content)
                .SingleAsync(cancellationToken);

    private async Task<List<ProtocolPartLine>> DescribeUsedPartsAsync(List<ServiceEntry> serviceEntries, CancellationToken cancellationToken)
    {
        var usedParts = ServiceEntry.SummarizeUsedParts(serviceEntries);
        var partIds = usedParts.Select(usedPart => usedPart.PartId).Distinct().ToList();
        var parts = await dbContext.Parts
            .AsNoTracking()
            .Where(part => partIds.Contains(part.Id))
            .ToDictionaryAsync(part => part.Id, cancellationToken);

        return [.. usedParts
            .Select(usedPart => new ProtocolPartLine(parts[usedPart.PartId].Name, parts[usedPart.PartId].CatalogNumber, usedPart.Quantity, usedPart.UnitPrice))
            .OrderBy(line => line.Name, StringComparer.Ordinal)
            .ThenBy(line => line.UnitPrice)];
    }
}
