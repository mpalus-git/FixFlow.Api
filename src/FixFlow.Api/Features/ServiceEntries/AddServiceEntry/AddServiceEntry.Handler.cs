using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.ServiceEntries.AddServiceEntry;

public sealed class AddServiceEntryHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<ServiceEntryResponse>> HandleAsync(
        Guid workOrderId,
        AddServiceEntryRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders
            .VisibleTo(user)
            .SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var partUsages = await LoadPartUsagesAsync(request.Parts ?? [], cancellationToken);
        if (partUsages.IsError)
        {
            return partUsages.Errors;
        }

        var previousEntries = request.IsCorrection
            ? await dbContext.ServiceEntries.AsNoTracking().Where(entry => entry.WorkOrderId == workOrderId).ToListAsync(cancellationToken)
            : [];
        var technicianId = user.GetUserId();
        var photoUrls = request.PhotoUrls ?? [];
        var now = timeProvider.GetUtcNow();
        var creation = request switch
        {
            { IsCorrection: true } => ServiceEntry.CreateCorrection(workOrder, technicianId, request.Note, photoUrls, partUsages.Value, previousEntries, now),
            { WorkStartedAt: { } workStartedAt, WorkFinishedAt: { } workFinishedAt } => ServiceEntry.CreateWork(
                workOrder,
                technicianId,
                request.Note,
                photoUrls,
                workStartedAt.ToDatabasePrecision(),
                workFinishedAt.ToDatabasePrecision(),
                request is { Latitude: { } latitude, Longitude: { } longitude } ? new GpsLocation(latitude, longitude) : null,
                partUsages.Value,
                now),
            _ => ServiceEntryErrors.WorkTimeRequired,
        };
        if (creation.IsError)
        {
            return creation.Errors;
        }

        dbContext.ServiceEntries.Add(creation.Value);
        dbContext.RejectSaveIfChangedConcurrently(workOrder);
        var saving = await dbContext.SaveChangesOrConflictAsync(
            PartConfiguration.StockQuantityCheckName,
            PartErrors.InsufficientStock,
            cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        var usedParts = partUsages.Value.Select(usage => usage.Part).DistinctBy(part => part.Id).ToDictionary(part => part.Id);
        return ServiceEntryResponse.FromDomain(creation.Value, usedParts);
    }

    private async Task<ErrorOr<List<PartUsage>>> LoadPartUsagesAsync(IReadOnlyList<ServiceEntryPartRequest> requestedParts, CancellationToken cancellationToken)
    {
        var partIds = requestedParts.Select(part => part.PartId).ToList();
        var parts = await dbContext.Parts
            .Where(part => partIds.Contains(part.Id))
            .ToDictionaryAsync(part => part.Id, cancellationToken);
        if (parts.Count != partIds.Count)
        {
            return PartErrors.NotFound;
        }

        return requestedParts.Select(part => new PartUsage(parts[part.PartId], part.Quantity)).ToList();
    }
}
