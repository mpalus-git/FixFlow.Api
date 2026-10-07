using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.IntegrationTests.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Persistence;

public sealed class WorkOrderConcurrencyTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Concurrent_Modification_When_Work_Order_Was_Changed_After_It_Was_Loaded()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
        var technician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);
        await using var dispatcherScope = Factory.Services.CreateAsyncScope();
        await using var technicianScope = Factory.Services.CreateAsyncScope();
        var dispatcherDbContext = dispatcherScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var technicianDbContext = technicianScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var workOrderSeenByDispatcher = await dispatcherDbContext.WorkOrders.SingleAsync(item => item.Id == workOrder.Id, TestContext.Current.CancellationToken);
        var workOrderSeenByTechnician = await technicianDbContext.WorkOrders.SingleAsync(item => item.Id == workOrder.Id, TestContext.Current.CancellationToken);

        workOrderSeenByDispatcher.Unassign(DateTimeOffset.UtcNow);
        var unassignment = await dispatcherDbContext.SaveChangesOrConflictAsync(TestContext.Current.CancellationToken);
        workOrderSeenByTechnician.Start(technician.Id, technicianHasWorkInProgress: false, DateTimeOffset.UtcNow);
        var start = await technicianDbContext.SaveChangesOrConflictAsync(TestContext.Current.CancellationToken);

        unassignment.IsError.ShouldBeFalse();
        start.FirstError.ShouldBe(SaveChangesConflicts.ConcurrentModification);
        var storedWorkOrder = await client.GetWorkOrderAsync(workOrder.Id);
        storedWorkOrder.Status.ShouldBe(WorkOrderStatus.New);
        storedWorkOrder.TechnicianId.ShouldBeNull();
    }
}
