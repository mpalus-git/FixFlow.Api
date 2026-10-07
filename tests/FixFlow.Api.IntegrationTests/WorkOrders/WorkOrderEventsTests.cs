using System.Net.Http.Json;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.ListWorkOrderEvents;
using FixFlow.Api.Features.WorkOrders.ReassignTechnician;
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

    [Fact]
    public async Task Should_Return_History_With_Names_When_Assigned_Technician_Lists_Events_Of_Reassigned_Work_Order()
    {
        var dispatcher = await CreateUserAsync(Roles.Dispatcher, "Katarzyna Nowak");
        var firstTechnician = await CreateUserAsync(Roles.Technician, "Anna Kowalczyk");
        var secondTechnician = await CreateUserAsync(Roles.Technician, "Tomasz Wójcik");
        using var dispatcherClient = await CreateAuthenticatedClientAsync(dispatcher);
        using var technicianClient = await CreateAuthenticatedClientAsync(secondTechnician);
        var workOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await dispatcherClient.CreateServicedDeviceAsync()));
        using var assignResponse = await dispatcherClient.PostAssignAsync(workOrder.Id, firstTechnician.Id);
        using var reassignResponse = await dispatcherClient.PostAsJsonAsync(
            new Uri($"/api/v1/work-orders/{workOrder.Id}/reassign", UriKind.Relative),
            new ReassignTechnicianRequest(secondTechnician.Id),
            TestContext.Current.CancellationToken);
        using var startResponse = await technicianClient.PostTransitionAsync(workOrder.Id, "start");
        using var repeatedStartResponse = await technicianClient.PostTransitionAsync(workOrder.Id, "start");

        var events = await technicianClient.GetFromJsonAsync<List<WorkOrderEventResponse>>(
            new Uri($"/api/v1/work-orders/{workOrder.Id}/events", UriKind.Relative),
            ApiJson.Options,
            TestContext.Current.CancellationToken);

        reassignResponse.EnsureSuccessStatusCode();
        repeatedStartResponse.EnsureSuccessStatusCode();
        events.ShouldNotBeNull();
        events.Select(item => item.Type).ShouldBe(
        [
            WorkOrderEventType.Created,
            WorkOrderEventType.Assigned,
            WorkOrderEventType.Reassigned,
            WorkOrderEventType.Started,
        ]);
        events.Select(item => item.ActorName).ShouldBe(["Katarzyna Nowak", "Katarzyna Nowak", "Katarzyna Nowak", "Tomasz Wójcik"]);
        events.Select(item => item.TechnicianName).ShouldBe([null, "Anna Kowalczyk", "Tomasz Wójcik", "Tomasz Wójcik"]);
        events.ShouldAllBe(item => item.DueDate == workOrder.DueDate);
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
