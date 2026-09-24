using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class ListWorkOrdersTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Requested_Page_Ordered_By_Due_Date_With_Total_Count_When_Work_Orders_Exceed_Page_Size()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();
        foreach (var dueInDays in new[] { 4, 1, 5, 3, 2 })
        {
            await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId, dueInDays) with { Description = $"Due in {dueInDays} days" });
        }

        var page = await client.ListWorkOrdersAsync("?page=2&pageSize=2");

        page.Items.Select(item => item.Description).ShouldBe(["Due in 3 days", "Due in 4 days"]);
        page.TotalCount.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Filter_Work_Orders_By_Status_Technician_And_Device_When_Filters_Are_Provided()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var firstDeviceId = await client.CreateServicedDeviceAsync("SN-1");
        var secondDeviceId = await client.CreateServicedDeviceAsync("SN-2");
        var newWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(firstDeviceId));
        var assignedWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(firstDeviceId));
        var otherDeviceWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(secondDeviceId));
        var technician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(assignedWorkOrder.Id, technician.Id);

        var byStatus = await client.ListWorkOrdersAsync($"?status={WorkOrderStatus.Assigned}");
        var byTechnician = await client.ListWorkOrdersAsync($"?technicianId={technician.Id}");
        var byDevice = await client.ListWorkOrdersAsync($"?deviceId={firstDeviceId}&status={WorkOrderStatus.New}");

        byStatus.Items.Select(item => item.Id).ShouldBe([assignedWorkOrder.Id]);
        byTechnician.Items.Select(item => item.Id).ShouldBe([assignedWorkOrder.Id]);
        byDevice.Items.Select(item => item.Id).ShouldBe([newWorkOrder.Id]);
        byDevice.Items.ShouldNotContain(item => item.Id == otherDeviceWorkOrder.Id);
    }

    [Fact]
    public async Task Should_List_Only_Own_Work_Orders_When_Technician_Lists_Work_Orders()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await dispatcherClient.CreateServicedDeviceAsync();
        var ownWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        var otherWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        var technician = await CreateUserAsync(Roles.Technician);
        var otherTechnician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(ownWorkOrder.Id, technician.Id);
        await Factory.AssignTechnicianDirectlyAsync(otherWorkOrder.Id, otherTechnician.Id);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);

        var page = await technicianClient.ListWorkOrdersAsync();
        var pageFilteredByOtherTechnician = await technicianClient.ListWorkOrdersAsync($"?technicianId={otherTechnician.Id}");

        page.Items.Select(item => item.Id).ShouldBe([ownWorkOrder.Id]);
        page.TotalCount.ShouldBe(1);
        pageFilteredByOtherTechnician.TotalCount.ShouldBe(0);
    }
}
