using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.CompleteWorkOrder;

public sealed class CompleteWorkOrderHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<WorkOrderResponse>> HandleAsync(Guid workOrderId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders
            .VisibleTo(user)
            .SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var hasServiceEntries = await dbContext.ServiceEntries.AnyAsync(entry => entry.WorkOrderId == workOrderId, cancellationToken);

        var completion = workOrder.Complete(hasServiceEntries, timeProvider.GetUtcNow());
        if (completion.IsError)
        {
            return completion.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return WorkOrderResponse.FromDomain(workOrder);
    }
}
