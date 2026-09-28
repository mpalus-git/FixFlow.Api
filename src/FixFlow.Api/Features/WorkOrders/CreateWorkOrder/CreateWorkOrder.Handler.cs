using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.CreateWorkOrder;

public sealed class CreateWorkOrderHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var device = await dbContext.Devices
            .AsNoTracking()
            .SingleOrDefaultAsync(device => device.Id == request.DeviceId, cancellationToken);
        if (device is null)
        {
            return DeviceErrors.NotFound;
        }

        var creation = WorkOrder.Create(device, request.Description, request.Priority, request.DueDate.ToDatabasePrecision(), timeProvider.GetUtcNow());
        if (creation.IsError)
        {
            return creation.Errors;
        }

        dbContext.WorkOrders.Add(creation.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return dbContext.Versioned(creation.Value, WorkOrderResponse.FromDomain(creation.Value));
    }
}
