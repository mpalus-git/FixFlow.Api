using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.ServiceEntries.ListServiceEntries;

public sealed class ListServiceEntriesHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<List<ServiceEntryResponse>>> HandleAsync(Guid workOrderId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var workOrderIsVisible = await dbContext.WorkOrders
            .VisibleTo(user)
            .AnyAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (!workOrderIsVisible)
        {
            return WorkOrderErrors.NotFound;
        }

        var entries = await dbContext.ServiceEntries
            .AsNoTracking()
            .Where(entry => entry.WorkOrderId == workOrderId)
            .OrderBy(entry => entry.CreatedAt)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);

        var partIds = entries.SelectMany(entry => entry.Parts).Select(part => part.PartId).Distinct().ToList();
        var parts = await dbContext.Parts
            .AsNoTracking()
            .Where(part => partIds.Contains(part.Id))
            .ToDictionaryAsync(part => part.Id, cancellationToken);

        return entries.Select(entry => ServiceEntryResponse.FromDomain(entry, parts)).ToList();
    }
}
