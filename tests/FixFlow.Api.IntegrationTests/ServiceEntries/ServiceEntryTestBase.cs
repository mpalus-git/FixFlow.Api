using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.WorkOrders;
using FixFlow.Api.IntegrationTests.WorkOrders;

namespace FixFlow.Api.IntegrationTests.ServiceEntries;

public abstract class ServiceEntryTestBase(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    protected async Task<WorkOrderInProgress> CreateWorkOrderInProgressAsync()
    {
        var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician);
        var technicianClient = await CreateAuthenticatedClientAsync(technician);
        var workOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await dispatcherClient.CreateServicedDeviceAsync()));
        using var assignResponse = await dispatcherClient.PostAssignAsync(workOrder.Id, technician.Id);
        assignResponse.EnsureSuccessStatusCode();
        using var startResponse = await technicianClient.PostTransitionAsync(workOrder.Id, "start");
        startResponse.EnsureSuccessStatusCode();

        return new WorkOrderInProgress(dispatcherClient, technicianClient, await startResponse.ReadWorkOrderAsync());
    }

    protected async Task<WorkOrderInProgress> CreateWorkOrderInProgressWithServiceEntryAsync()
    {
        var scenario = await CreateWorkOrderInProgressAsync();
        await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.WorkEntry(scenario.WorkOrder));
        return scenario;
    }

    protected async Task<WorkOrderInProgress> CreateCompletedWorkOrderAsync()
    {
        var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();
        using var response = await scenario.TechnicianClient.PostTransitionAsync(scenario.WorkOrder.Id, "complete");
        response.EnsureSuccessStatusCode();
        return scenario;
    }
}

public sealed record WorkOrderInProgress(HttpClient DispatcherClient, HttpClient TechnicianClient, WorkOrderResponse WorkOrder) : IDisposable
{
    public void Dispose()
    {
        DispatcherClient.Dispose();
        TechnicianClient.Dispose();
    }
}
