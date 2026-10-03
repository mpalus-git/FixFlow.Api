using System.Globalization;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using FixFlow.Api.IntegrationTests.Clients;
using FixFlow.Api.IntegrationTests.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        page.Items.Select(item => item.TechnicianName).ShouldBe([technician.FullName, null]);
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
    [InlineData("/0001", "Biuro Alfa")]
    [InlineData("0002", "Hotel Beta")]
    public async Task Should_Find_Work_Orders_By_Number_Description_Serial_Number_Or_Client_Name_When_Search_Is_Provided(string search, string expectedClientName)
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
    public async Task Should_Return_Work_Orders_Of_All_Client_Devices_When_Client_Filter_Is_Provided()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient("Biuro Alfa"));
        var activeDevice = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-ALFA-1"));
        var archivedDevice = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-ALFA-2"));
        var activeDeviceWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(activeDevice.Id, dueInDays: 1));
        var archivedDeviceWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(archivedDevice.Id, dueInDays: 2));
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync("SN-OTHER")));
        var technician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(archivedDeviceWorkOrder.Id, technician.Id);
        using var archiveResponse = await client.PostAsync(new Uri($"/api/v1/devices/{archivedDevice.Id}/archive", UriKind.Relative), null, TestContext.Current.CancellationToken);
        archiveResponse.EnsureSuccessStatusCode();

        var byClient = await client.ListWorkOrdersAsync($"?clientId={owner.Id}");
        var byClientAndStatus = await client.ListWorkOrdersAsync($"?clientId={owner.Id}&status={WorkOrderStatus.Assigned}");
        var byUnknownClient = await client.ListWorkOrdersAsync($"?clientId={Guid.NewGuid()}");

        byClient.Items.Select(item => item.Id).ShouldBe([activeDeviceWorkOrder.Id, archivedDeviceWorkOrder.Id]);
        byClientAndStatus.Items.Select(item => item.Id).ShouldBe([archivedDeviceWorkOrder.Id]);
        byUnknownClient.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_List_Only_Own_Work_Orders_Of_Client_When_Technician_Filters_By_Client()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await dispatcherClient.CreateClientAsync(ClientRequests.NewClient("Biuro Alfa"));
        var device = await dispatcherClient.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-ALFA-1"));
        var ownWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(device.Id));
        var otherWorkOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(device.Id));
        var technician = await CreateUserAsync(Roles.Technician);
        var otherTechnician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(ownWorkOrder.Id, technician.Id);
        await Factory.AssignTechnicianDirectlyAsync(otherWorkOrder.Id, otherTechnician.Id);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);

        var page = await technicianClient.ListWorkOrdersAsync($"?clientId={owner.Id}");

        page.Items.Select(item => item.Id).ShouldBe([ownWorkOrder.Id]);
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

    [Theory]
    [InlineData("DueDate", "Asc", new[] { 2, 4, 0, 3, 1 })]
    [InlineData("DueDate", "Desc", new[] { 1, 3, 0, 4, 2 })]
    [InlineData("CreatedAt", "Asc", new[] { 0, 1, 2, 3, 4 })]
    [InlineData("CreatedAt", "Desc", new[] { 4, 3, 2, 1, 0 })]
    [InlineData("Status", "Asc", new[] { 1, 3, 4, 2, 0 })]
    [InlineData("Status", "Desc", new[] { 0, 2, 4, 3, 1 })]
    [InlineData("ClientName", "Asc", new[] { 1, 2, 4, 0, 3 })]
    [InlineData("ClientName", "Desc", new[] { 3, 0, 4, 2, 1 })]
    public async Task Should_Order_Work_Orders_By_Requested_Field_And_Direction_When_Sorting_Is_Provided(string sortBy, string sortDirection, int[] expectedCreationOrder)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrderIds = await CreateWorkOrdersToSortAsync(client);

        var page = await client.ListWorkOrdersAsync($"?sortBy={sortBy}&sortDirection={sortDirection}");

        page.Items.Select(item => item.Id).ShouldBe(expectedCreationOrder.Select(index => workOrderIds[index]));
    }

    [Theory]
    [InlineData("Asc", new[] { WorkOrderPriority.Low, WorkOrderPriority.Normal, WorkOrderPriority.High, WorkOrderPriority.High, WorkOrderPriority.Critical })]
    [InlineData("Desc", new[] { WorkOrderPriority.Critical, WorkOrderPriority.High, WorkOrderPriority.High, WorkOrderPriority.Normal, WorkOrderPriority.Low })]
    public async Task Should_Order_Work_Orders_By_Priority_Importance_When_Sorting_By_Priority(string sortDirection, WorkOrderPriority[] expectedPriorities)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await CreateWorkOrdersToSortAsync(client);

        var page = await client.ListWorkOrdersAsync($"?sortBy=Priority&sortDirection={sortDirection}");

        page.Items.Select(item => item.Priority).ShouldBe(expectedPriorities);
    }

    [Theory]
    [InlineData("DueDate", "Asc")]
    [InlineData("Priority", "Desc")]
    public async Task Should_Return_Each_Work_Order_Once_Ordered_By_Identifier_When_Sorted_Values_Are_Equal_Across_Pages(string sortBy, string sortDirection)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();
        var sharedDueDate = DateTimeOffset.UtcNow.AddDays(3).ToDatabasePrecision();
        var workOrderIds = new List<Guid>();
        for (var index = 0; index < 5; index++)
        {
            var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId) with { DueDate = sharedDueDate });
            workOrderIds.Add(workOrder.Id);
        }

        var listedIds = new List<Guid>();
        for (var pageNumber = 1; pageNumber <= 3; pageNumber++)
        {
            var page = await client.ListWorkOrdersAsync($"?page={pageNumber}&pageSize=2&sortBy={sortBy}&sortDirection={sortDirection}");
            listedIds.AddRange(page.Items.Select(item => item.Id));
        }

        var idsInDatabaseOrder = workOrderIds.OrderBy(id => id.ToString(), StringComparer.Ordinal);
        listedIds.ShouldBe(sortDirection == "Desc" ? idsInDatabaseOrder.Reverse() : idsInDatabaseOrder);
    }

    private async Task<Guid[]> CreateWorkOrdersToSortAsync(HttpClient client)
    {
        (string ClientName, int DueInDays, WorkOrderPriority Priority, WorkOrderStatus Status)[] workOrders =
        [
            ("Serwis Gamma", 3, WorkOrderPriority.Normal, WorkOrderStatus.Invoiced),
            ("Biuro Alfa", 5, WorkOrderPriority.Critical, WorkOrderStatus.New),
            ("Hotel Beta", 1, WorkOrderPriority.Low, WorkOrderStatus.Completed),
            ("Zaklad Delta", 4, WorkOrderPriority.High, WorkOrderStatus.Assigned),
            ("Kawiarnia Epsilon", 2, WorkOrderPriority.High, WorkOrderStatus.InProgress),
        ];
        var technician = await CreateUserAsync(Roles.Technician);
        var workOrderIds = new List<Guid>();
        foreach (var (clientName, dueInDays, priority, status) in workOrders)
        {
            var owner = await client.CreateClientAsync(ClientRequests.NewClient(clientName));
            var device = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, $"SN-SORT-{workOrderIds.Count}"));
            var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(device.Id, dueInDays) with { Priority = priority });
            await MoveToStatusDirectlyAsync(workOrder.Id, status, technician.Id);
            workOrderIds.Add(workOrder.Id);
        }

        return [.. workOrderIds];
    }

    private async Task MoveToStatusDirectlyAsync(Guid workOrderId, WorkOrderStatus status, Guid technicianId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var workOrder = await dbContext.WorkOrders.SingleAsync(item => item.Id == workOrderId, TestContext.Current.CancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (status >= WorkOrderStatus.Assigned)
        {
            workOrder.Assign(technicianId).IsError.ShouldBeFalse();
        }

        if (status >= WorkOrderStatus.InProgress)
        {
            workOrder.Start(technicianId, technicianHasWorkInProgress: false, now).IsError.ShouldBeFalse();
        }

        if (status >= WorkOrderStatus.Completed)
        {
            workOrder.Complete(hasServiceEntries: true, now).IsError.ShouldBeFalse();
        }

        if (status >= WorkOrderStatus.Invoiced)
        {
            workOrder.Invoice(now).IsError.ShouldBeFalse();
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
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
