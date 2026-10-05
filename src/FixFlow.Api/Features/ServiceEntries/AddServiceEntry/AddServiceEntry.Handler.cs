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
using Npgsql;

namespace FixFlow.Api.Features.ServiceEntries.AddServiceEntry;

public sealed class AddServiceEntryHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<AddedServiceEntry>> HandleAsync(
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

        var technicianId = user.GetUserId();
        if (request.Id is { } requestedId && await FindRetriedEntryAsync(requestedId, workOrderId, technicianId, cancellationToken) is { } retriedEntry)
        {
            return retriedEntry;
        }

        var partUsages = await LoadPartUsagesAsync(request.Parts ?? [], cancellationToken);
        if (partUsages.IsError)
        {
            return partUsages.Errors;
        }

        var previousEntries = request.IsCorrection
            ? await dbContext.ServiceEntries.AsNoTracking().Where(entry => entry.WorkOrderId == workOrderId).ToListAsync(cancellationToken)
            : [];
        var photoUrls = request.PhotoUrls ?? [];
        var now = timeProvider.GetUtcNow();
        var creation = request switch
        {
            { IsCorrection: true } => ServiceEntry.CreateCorrection(workOrder, technicianId, request.Note, photoUrls, partUsages.Value, previousEntries, now, request.Id),
            { WorkStartedAt: { } workStartedAt, WorkFinishedAt: { } workFinishedAt } => ServiceEntry.CreateWork(
                workOrder,
                technicianId,
                request.Note,
                photoUrls,
                workStartedAt.ToDatabasePrecision(),
                workFinishedAt.ToDatabasePrecision(),
                request is { Latitude: { } latitude, Longitude: { } longitude } ? new GpsLocation(latitude, longitude) : null,
                partUsages.Value,
                now,
                request.Id),
            _ => ServiceEntryErrors.WorkTimeRequired,
        };
        if (creation.IsError)
        {
            return creation.Errors;
        }

        dbContext.ServiceEntries.Add(creation.Value);
        dbContext.RejectSaveIfChangedConcurrently(workOrder);
        var saving = await SaveChangesAsync(cancellationToken);
        if (saving.IsError)
        {
            return request.Id is { } savedId && await FindRetriedEntryAsync(savedId, workOrderId, technicianId, cancellationToken) is { } concurrentlyAddedEntry
                ? concurrentlyAddedEntry
                : saving.Errors;
        }

        return await ToAddedServiceEntryAsync(creation.Value, wasAlreadyAdded: false, cancellationToken);
    }

    private async Task<ErrorOr<Success>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesOrConflictAsync(PartConfiguration.StockQuantityCheckName, PartErrors.InsufficientStock, cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: ServiceEntryConfiguration.PrimaryKeyName })
        {
            return SaveChangesConflicts.ConcurrentModification;
        }
    }

    private async Task<ErrorOr<AddedServiceEntry>?> FindRetriedEntryAsync(Guid entryId, Guid workOrderId, Guid technicianId, CancellationToken cancellationToken)
    {
        var existingEntry = await dbContext.ServiceEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == entryId, cancellationToken);
        if (existingEntry is null)
        {
            return null;
        }

        var retry = existingEntry.EnsureIsRetryOf(workOrderId, technicianId);
        if (retry.IsError)
        {
            return retry.Errors;
        }

        return await ToAddedServiceEntryAsync(existingEntry, wasAlreadyAdded: true, cancellationToken);
    }

    private async Task<AddedServiceEntry> ToAddedServiceEntryAsync(ServiceEntry entry, bool wasAlreadyAdded, CancellationToken cancellationToken)
    {
        var responses = await dbContext.ToServiceEntryResponsesAsync([entry], cancellationToken);
        return new AddedServiceEntry(responses.Single(), wasAlreadyAdded);
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

public sealed record AddedServiceEntry(ServiceEntryResponse Entry, bool WasAlreadyAdded);
