using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class CreateAndGetWorkOrderTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData(Roles.Dispatcher)]
    [InlineData(Roles.Admin)]
    public async Task Should_Create_New_Unassigned_Work_Order_With_Location_When_Dispatcher_Or_Admin_Creates_It(string role)
    {
        using var client = await CreateAuthenticatedClientAsync(role);
        var deviceId = await client.CreateServicedDeviceAsync();
        var request = WorkOrderRequests.NewWorkOrder(deviceId);

        using var response = await client.PostWorkOrderAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var workOrder = await response.ReadWorkOrderAsync();
        response.Headers.Location.ShouldBe(WorkOrderRequests.WorkOrderUri(workOrder.Id));
        workOrder.DeviceId.ShouldBe(deviceId);
        workOrder.Priority.ShouldBe(WorkOrderPriority.High);
        workOrder.Status.ShouldBe(WorkOrderStatus.New);
        workOrder.TechnicianId.ShouldBeNull();
        workOrder.StartedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Use_Normal_Priority_When_Priority_Is_Omitted()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();
        var request = new { deviceId, description = "Printer jams paper", dueDate = DateTimeOffset.UtcNow.AddDays(1) };

        using var response = await client.PostAsJsonAsync(WorkOrderRequests.WorkOrdersUri, request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.ReadWorkOrderAsync()).Priority.ShouldBe(WorkOrderPriority.Normal);
    }

    [Fact]
    public async Task Should_Store_Due_Date_In_Utc_When_Due_Date_Has_Time_Zone_Offset()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();
        var localDueDate = new DateTimeOffset(DateTime.UtcNow.Year + 1, 3, 10, 14, 0, 0, TimeSpan.FromHours(2)).AddTicks(7);

        var createdWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId) with { DueDate = localDueDate });

        var storedWorkOrder = await client.GetWorkOrderAsync(createdWorkOrder.Id);
        storedWorkOrder.DueDate.Offset.ShouldBe(TimeSpan.Zero);
        storedWorkOrder.DueDate.ShouldBe(localDueDate.AddTicks(-7));
        createdWorkOrder.ShouldBe(storedWorkOrder);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Due_Date_Is_In_The_Past()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();

        using var response = await client.PostWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId, dueInDays: -1));

        await response.ShouldBeValidationProblemAsync("DueDate");
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Description_Is_Empty()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();

        using var response = await client.PostWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId) with { Description = string.Empty });

        await response.ShouldBeValidationProblemAsync("Description");
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Device_Does_Not_Exist()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await client.PostWorkOrderAsync(WorkOrderRequests.NewWorkOrder(Guid.CreateVersion7()));

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, DeviceErrors.NotFound.Code);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Device_Is_Archived()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();
        using var archiveResponse = await client.PostAsync(new Uri($"/api/v1/devices/{deviceId}/archive", UriKind.Relative), null, TestContext.Current.CancellationToken);

        using var response = await client.PostWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));

        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.DeviceArchived.Code);
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_Technician_Creates_Work_Order()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await client.PostWorkOrderAsync(WorkOrderRequests.NewWorkOrder(Guid.CreateVersion7()));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Return_Work_Order_When_Dispatcher_Gets_It()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));

        var fetchedWorkOrder = await client.GetWorkOrderAsync(createdWorkOrder.Id);

        fetchedWorkOrder.ShouldBe(createdWorkOrder);
    }

    [Fact]
    public async Task Should_Return_Work_Order_When_Technician_Gets_Own_Work_Order()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await dispatcherClient.CreateServicedDeviceAsync()));
        var technician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);

        var fetchedWorkOrder = await technicianClient.GetWorkOrderAsync(workOrder.Id);

        fetchedWorkOrder.TechnicianId.ShouldBe(technician.Id);
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Technician_Gets_Work_Order_Of_Another_Technician()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await dispatcherClient.CreateServicedDeviceAsync()));
        var otherTechnician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, otherTechnician.Id);
        using var technicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await technicianClient.GetAsync(WorkOrderRequests.WorkOrderUri(workOrder.Id), TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, WorkOrderErrors.NotFound.Code);
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Work_Order_Does_Not_Exist()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await client.GetAsync(WorkOrderRequests.WorkOrderUri(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, WorkOrderErrors.NotFound.Code);
    }
}
