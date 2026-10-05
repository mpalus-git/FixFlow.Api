using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Devices.ListDeviceWorkOrders;
using FixFlow.Api.IntegrationTests.ServiceEntries;
using FixFlow.Api.IntegrationTests.WorkOrders;

namespace FixFlow.Api.IntegrationTests.Devices;

public sealed class DeviceWorkOrderHistoryTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Work_Orders_Of_All_Technicians_Newest_First_With_Service_Entry_Count_When_Dispatcher_Requests_History()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await dispatcherClient.CreateServicedDeviceAsync();
        var otherDeviceId = await dispatcherClient.CreateServicedDeviceAsync("AC-2002");
        var firstTechnician = await CreateUserAsync(Roles.Technician, "Jan Kowalski");
        var secondTechnician = await CreateUserAsync(Roles.Technician, "Anna Nowak");
        var startedWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        var assignedWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        var newWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(otherDeviceId));
        await StartWorkWithServiceEntryAsync(dispatcherClient, startedWorkOrder.Id, firstTechnician);
        await Factory.AssignTechnicianDirectlyAsync(assignedWorkOrder.Id, secondTechnician.Id);

        var history = await dispatcherClient.GetDeviceHistoryAsync(deviceId);

        history.TotalCount.ShouldBe(3);
        history.Items.Select(item => item.Id).ShouldBe([newWorkOrder.Id, assignedWorkOrder.Id, startedWorkOrder.Id]);
        history.Items.Select(item => item.TechnicianName).ShouldBe([null, "Anna Nowak", "Jan Kowalski"]);
        history.Items.Select(item => item.ServiceEntryCount).ShouldBe([0, 0, 1]);
        history.Items[2].Number.ShouldBe(startedWorkOrder.Number);
        history.Items[2].StartedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Return_History_When_Admin_Requests_Archived_Device()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var deviceId = await adminClient.CreateServicedDeviceAsync();
        var workOrder = await adminClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        await adminClient.ArchiveDeviceAsync(deviceId);

        var history = await adminClient.GetDeviceHistoryAsync(deviceId);

        history.Items.Select(item => item.Id).ShouldBe([workOrder.Id]);
    }

    [Fact]
    public async Task Should_Return_Only_Own_Work_Orders_When_Technician_Requests_History_Of_Archived_Device()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician);
        var otherTechnician = await CreateUserAsync(Roles.Technician);
        var deviceId = await dispatcherClient.CreateServicedDeviceAsync();
        var ownWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        var foreignWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        await Factory.AssignTechnicianDirectlyAsync(ownWorkOrder.Id, technician.Id);
        await Factory.AssignTechnicianDirectlyAsync(foreignWorkOrder.Id, otherTechnician.Id);
        await dispatcherClient.ArchiveDeviceAsync(deviceId);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);

        var history = await technicianClient.GetDeviceHistoryAsync(deviceId);

        history.TotalCount.ShouldBe(1);
        history.Items.Select(item => item.Id).ShouldBe([ownWorkOrder.Id]);
    }

    [Fact]
    public async Task Should_Return_Not_Found_When_Technician_Has_No_Work_Order_On_Device()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var otherTechnician = await CreateUserAsync(Roles.Technician);
        var deviceId = await dispatcherClient.CreateServicedDeviceAsync();
        var foreignWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        await Factory.AssignTechnicianDirectlyAsync(foreignWorkOrder.Id, otherTechnician.Id);
        using var technicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await technicianClient.GetAsync(DeviceHistoryRequests.DeviceHistoryUri(deviceId), TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, DeviceErrors.NotFound.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Page_Size_Exceeds_Limit()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await dispatcherClient.CreateServicedDeviceAsync();

        using var response = await dispatcherClient.GetAsync(DeviceHistoryRequests.DeviceHistoryUri(deviceId, "?pageSize=101"), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("pageSize");
    }

    private async Task StartWorkWithServiceEntryAsync(HttpClient dispatcherClient, Guid workOrderId, TestUser technician)
    {
        using var assignResponse = await dispatcherClient.PostAssignAsync(workOrderId, technician.Id);
        assignResponse.EnsureSuccessStatusCode();
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        using var startResponse = await technicianClient.PostTransitionAsync(workOrderId, "start");
        startResponse.EnsureSuccessStatusCode();
        var startedWorkOrder = await startResponse.ReadWorkOrderAsync();
        await technicianClient.AddServiceEntryAsync(workOrderId, ServiceEntryRequests.WorkEntry(startedWorkOrder));
    }
}

file static class DeviceHistoryRequests
{
    public static Uri DeviceHistoryUri(Guid deviceId, string query = "") => new($"/api/v1/devices/{deviceId}/work-orders{query}", UriKind.Relative);

    public static async Task<PagedResponse<DeviceWorkOrderHistoryItemResponse>> GetDeviceHistoryAsync(this HttpClient client, Guid deviceId)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<DeviceWorkOrderHistoryItemResponse>>(
            DeviceHistoryUri(deviceId),
            ApiJson.Options,
            TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }

    public static async Task ArchiveDeviceAsync(this HttpClient client, Guid deviceId)
    {
        using var response = await client.PostAsync(new Uri($"/api/v1/devices/{deviceId}/archive", UriKind.Relative), null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
