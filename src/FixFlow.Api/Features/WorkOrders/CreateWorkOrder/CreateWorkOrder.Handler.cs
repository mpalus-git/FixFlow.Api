using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.CreateWorkOrder;

public sealed class CreateWorkOrderHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(CreateWorkOrderRequest request, Guid actorId, CancellationToken cancellationToken) =>
        dbContext.Database.CreateExecutionStrategy().ExecuteAsync(new WorkOrderCreation(request, actorId), CreateWorkOrderInTransactionAsync, cancellationToken);

    private async Task<ErrorOr<Versioned<WorkOrderResponse>>> CreateWorkOrderInTransactionAsync(WorkOrderCreation workOrderCreation, CancellationToken cancellationToken)
    {
        var request = workOrderCreation.Request;
        dbContext.ChangeTracker.Clear();
        var device = await dbContext.Devices
            .AsNoTracking()
            .SingleOrDefaultAsync(device => device.Id == request.DeviceId, cancellationToken);
        if (device is null)
        {
            return DeviceErrors.NotFound;
        }

        var creation = WorkOrder.Create(device, request.Description, request.Priority, request.DueDate.ToDatabasePrecision(), timeProvider.GetUtcNow(), workOrderCreation.ActorId);
        if (creation.IsError)
        {
            return creation.Errors;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.AssignNextNumberAsync(creation.Value, cancellationToken);
        dbContext.WorkOrders.Add(creation.Value);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await dbContext.VersionedWorkOrderResponseAsync(creation.Value, cancellationToken);
    }

    private sealed record WorkOrderCreation(CreateWorkOrderRequest Request, Guid ActorId);
}
