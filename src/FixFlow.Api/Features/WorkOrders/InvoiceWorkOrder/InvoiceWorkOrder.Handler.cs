using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.InvoiceWorkOrder;

public sealed class InvoiceWorkOrderHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<WorkOrderResponse>> HandleAsync(Guid workOrderId, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders.SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var invoicing = workOrder.Invoice(timeProvider.GetUtcNow());
        if (invoicing.IsError)
        {
            return invoicing.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return WorkOrderResponse.FromDomain(workOrder);
    }
}
