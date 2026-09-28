using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.UnassignTechnician;

public sealed class UnassignTechnicianHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(Guid workOrderId, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders.SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var unassignment = workOrder.Unassign();
        if (unassignment.IsError)
        {
            return unassignment.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return dbContext.Versioned(workOrder, WorkOrderResponse.FromDomain(workOrder));
    }
}
