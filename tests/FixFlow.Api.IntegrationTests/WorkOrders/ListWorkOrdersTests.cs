using System.Globalization;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using FixFlow.Api.IntegrationTests.Clients;
using FixFlow.Api.IntegrationTests.Devices;

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
    public async Task Should_Include_Device_Client_And_Technician_Details_When_Work_Orders_Are_Listed()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient("Biuro Rachunkowe Alfa"));
        var device = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-LIST-1"));
        var assignedWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(device.Id, dueInDays: 1));
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(device.Id, dueInDays: 2));
        var technician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(assignedWorkOrder.Id, technician.Id);

        var page = await client.ListWorkOrdersAsync();

        page.Items.Count.ShouldBe(2);
        page.Items.ShouldAllBe(item => item.DeviceSerialNumber == device.SerialNumber && item.DeviceModel == device.Model);
        page.Items.ShouldAllBe(item => item.ClientId == owner.Id && item.ClientName == owner.Name);
        page.Items.Select(item => item.TechnicianEmail).ShouldBe([technician.Email, null]);
    }

    [Fact]
    public async Task Should_Return_Work_Orders_Due_Within_Calendar_Days_When_Due_Date_Range_Is_Provided()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();
        var firstDay = DateOnly.FromDateTime(BusinessTime.From(DateTimeOffset.UtcNow).DateTime).AddDays(5);
        var lastDay = firstDay.AddDays(1);
        var dayAfterRange = BusinessTime.StartOfDay(lastDay.AddDays(1));
        await CreateWorkOrderDueAtAsync(client, deviceId, BusinessTime.StartOfDay(firstDay).AddMinutes(-1));
        var dueAtRangeStart = await CreateWorkOrderDueAtAsync(client, deviceId, BusinessTime.StartOfDay(firstDay));
        var dueAtRangeEnd = await CreateWorkOrderDueAtAsync(client, deviceId, dayAfterRange.AddMinutes(-1));
        await CreateWorkOrderDueAtAsync(client, deviceId, dayAfterRange);

        var page = await client.ListWorkOrdersAsync($"?dueFrom={FormatDate(firstDay)}&dueTo={FormatDate(lastDay)}");

        page.Items.Select(item => item.Id).ShouldBe([dueAtRangeStart.Id, dueAtRangeEnd.Id]);
    }

    [Theory]
    [InlineData("JAMS", "Biuro Alfa")]
    [InlineData("beta-2", "Hotel Beta")]
    [InlineData("hotel", "Hotel Beta")]
    public async Task Should_Find_Work_Orders_By_Description_Serial_Number_Or_Client_Name_When_Search_Is_Provided(string search, string expectedClientName)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await CreateWorkOrderForClientAsync(client, "Biuro Alfa", "SN-ALFA-1", "Printer jams paper");
        await CreateWorkOrderForClientAsync(client, "Hotel Beta", "SN-BETA-2", "Air conditioner is leaking");

        var page = await client.ListWorkOrdersAsync($"?search={search}");

        page.Items.ShouldHaveSingleItem().ClientName.ShouldBe(expectedClientName);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Due_Date_Range_Ends_Before_It_Starts()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await client.GetAsync(new Uri("/api/v1/work-orders?dueFrom=2026-10-02&dueTo=2026-10-01", UriKind.Relative), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("dueTo");
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

    private static Task<WorkOrderResponse> CreateWorkOrderDueAtAsync(HttpClient client, Guid deviceId, DateTimeOffset dueDate) =>
        client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId) with { DueDate = dueDate });

    private static async Task CreateWorkOrderForClientAsync(HttpClient client, string clientName, string serialNumber, string description)
    {
        var owner = await client.CreateClientAsync(ClientRequests.NewClient(clientName));
        var device = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, serialNumber));
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(device.Id) with { Description = description });
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
