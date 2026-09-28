using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.GetWorkOrder;

public sealed class GetWorkOrderHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(Guid workOrderId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders
            .VisibleTo(user)
            .SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);

        return workOrder is null ? WorkOrderErrors.NotFound : dbContext.Versioned(workOrder, WorkOrderResponse.FromDomain(workOrder));
    }
}
