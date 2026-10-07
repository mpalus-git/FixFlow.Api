using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class WorkOrderEventsTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Store_Events_With_Actors_When_Work_Order_Goes_Through_Transitions()
    {
        var dispatcher = await CreateUserAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician);
        using var dispatcherClient = await CreateAuthenticatedClientAsync(dispatcher);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        var workOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await dispatcherClient.CreateServicedDeviceAsync()));
        using var assignResponse = await dispatcherClient.PostAssignAsync(workOrder.Id, technician.Id);
        using var startResponse = await technicianClient.PostTransitionAsync(workOrder.Id, "start");

        var events = await LoadEventsAsync(workOrder.Id);

        assignResponse.EnsureSuccessStatusCode();
        startResponse.EnsureSuccessStatusCode();
        events.Select(item => item.Type).ShouldBe([WorkOrderEventType.Created, WorkOrderEventType.Assigned, WorkOrderEventType.Started]);
        events.Select(item => item.ActorId).ShouldBe([dispatcher.Id, dispatcher.Id, technician.Id]);
        events.Select(item => item.TechnicianId).ShouldBe([null, technician.Id, technician.Id]);
    }

    private async Task<List<WorkOrderEvent>> LoadEventsAsync(Guid workOrderId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        return await dbContext.WorkOrderEvents
            .Where(item => item.WorkOrderId == workOrderId)
            .OrderBy(item => item.OccurredAt)
            .ThenBy(item => item.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }
}
