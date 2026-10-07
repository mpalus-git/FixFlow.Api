using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.AssignTechnician;

public sealed class AssignTechnicianHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(Guid workOrderId, AssignTechnicianRequest request, CancellationToken cancellationToken)
    {
        var row = await dbContext.WorkOrders.FindRowAsync(workOrderId, dbContext, cancellationToken);
        if (row is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var workOrder = row.WorkOrder;

        var technician = await dbContext.FindActiveTechnicianAsync(request.TechnicianId, cancellationToken);
        if (technician is null)
        {
            return WorkOrderErrors.TechnicianNotFound;
        }

        var assignment = workOrder.Assign(request.TechnicianId, request.DueDate?.ToDatabasePrecision(), timeProvider.GetUtcNow());
        if (assignment.IsError)
        {
            return assignment.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return dbContext.VersionedResponse(row.WithTechnician(technician));
    }
}
