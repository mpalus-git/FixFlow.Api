using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.UnassignTechnician;

public sealed class UnassignTechnicianHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(Guid workOrderId, Guid actorId, CancellationToken cancellationToken)
    {
        var row = await dbContext.WorkOrders.FindRowAsync(workOrderId, dbContext, cancellationToken);
        if (row is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var workOrder = row.WorkOrder;

        var unassignment = workOrder.Unassign(timeProvider.GetUtcNow(), actorId);
        if (unassignment.IsError)
        {
            return unassignment.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return dbContext.VersionedResponse(row.WithTechnician(null));
    }
}
