using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.ServiceEntries.GetServiceEntry;

public sealed class GetServiceEntryHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<ServiceEntryResponse>> HandleAsync(Guid workOrderId, Guid serviceEntryId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var workOrderIsVisible = await dbContext.WorkOrders
            .VisibleTo(user)
            .AnyAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (!workOrderIsVisible)
        {
            return WorkOrderErrors.NotFound;
        }

        var entry = await dbContext.ServiceEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == serviceEntryId && entry.WorkOrderId == workOrderId, cancellationToken);
        if (entry is null)
        {
            return ServiceEntryErrors.NotFound;
        }

        var responses = await dbContext.ToServiceEntryResponsesAsync([entry], cancellationToken);
        return responses.Single();
    }
}
