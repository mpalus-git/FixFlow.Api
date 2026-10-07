using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using FixFlow.Api.Features.WorkOrders.AssignTechnician;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class AssignAndUnassignTechnicianTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Assign_Technician_And_Make_Work_Order_Visible_To_Technician_When_Dispatcher_Assigns()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);
        var technician = await CreateUserAsync(Roles.Technician);

        using var response = await client.PostAssignAsync(workOrder.Id, technician.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var assignedWorkOrder = await response.ReadWorkOrderAsync();
        assignedWorkOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        assignedWorkOrder.TechnicianId.ShouldBe(technician.Id);
        assignedWorkOrder.TechnicianEmail.ShouldBe(technician.Email);
        assignedWorkOrder.TechnicianName.ShouldBe(technician.FullName);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        (await technicianClient.GetWorkOrderAsync(workOrder.Id)).ShouldBe(assignedWorkOrder);
    }

    [Fact]
    public async Task Should_Change_Due_Date_When_Dispatcher_Assigns_Technician_With_New_Due_Date()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);
        var technician = await CreateUserAsync(Roles.Technician);
        var newDueDate = workOrder.DueDate.AddDays(4);

        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/work-orders/{workOrder.Id}/assign", UriKind.Relative),
            new AssignTechnicianRequest(technician.Id, newDueDate),
            ApiJson.Options,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var assignedWorkOrder = await response.ReadWorkOrderAsync();
        assignedWorkOrder.TechnicianId.ShouldBe(technician.Id);
        assignedWorkOrder.DueDate.ShouldBe(newDueDate);
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Assigned_User_Is_Not_Technician()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);
        var dispatcher = await CreateUserAsync(Roles.Dispatcher);

        using var notTechnicianResponse = await client.PostAssignAsync(workOrder.Id, dispatcher.Id);
        using var missingUserResponse = await client.PostAssignAsync(workOrder.Id, Guid.CreateVersion7());

        await notTechnicianResponse.ShouldBeProblemAsync(HttpStatusCode.NotFound, WorkOrderErrors.TechnicianNotFound.Code);
        await missingUserResponse.ShouldBeProblemAsync(HttpStatusCode.NotFound, WorkOrderErrors.TechnicianNotFound.Code);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Work_Order_Is_Already_Assigned()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);
        var technician = await CreateUserAsync(Roles.Technician);
        var otherTechnician = await CreateUserAsync(Roles.Technician);
        using var firstResponse = await client.PostAssignAsync(workOrder.Id, technician.Id);

        using var response = await client.PostAssignAsync(workOrder.Id, otherTechnician.Id);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "WorkOrder.InvalidStatusTransition");
        (await client.GetWorkOrderAsync(workOrder.Id)).TechnicianId.ShouldBe(technician.Id);
    }

    [Fact]
    public async Task Should_Return_To_New_And_Hide_From_Technician_When_Dispatcher_Unassigns_Technician()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);
        var technician = await CreateUserAsync(Roles.Technician);
        using var assignResponse = await client.PostAssignAsync(workOrder.Id, technician.Id);

        using var response = await client.PostTransitionAsync(workOrder.Id, "unassign");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var unassignedWorkOrder = await response.ReadWorkOrderAsync();
        unassignedWorkOrder.Status.ShouldBe(WorkOrderStatus.New);
        unassignedWorkOrder.TechnicianId.ShouldBeNull();
        unassignedWorkOrder.TechnicianEmail.ShouldBeNull();
        unassignedWorkOrder.TechnicianName.ShouldBeNull();
        response.ETag().ShouldBe(await client.GetETagAsync(WorkOrderRequests.WorkOrderUri(workOrder.Id), TestContext.Current.CancellationToken));
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        (await technicianClient.ListWorkOrdersAsync()).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Unassigning_New_Work_Order()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);

        using var response = await client.PostTransitionAsync(workOrder.Id, "unassign");

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "WorkOrder.InvalidStatusTransition");
    }

    private static async Task<WorkOrderResponse> CreateWorkOrderAsync(HttpClient client) =>
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
}
