using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.ReassignTechnician;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class ReassignTechnicianTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Move_Work_Order_To_Other_Technician_And_Date_When_Dispatcher_Reassigns_It()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
        var technician = await CreateUserAsync(Roles.Technician);
        var otherTechnician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);
        var newDueDate = workOrder.DueDate.AddDays(2);

        using var response = await PostReassignAsync(client, workOrder.Id, new ReassignTechnicianRequest(otherTechnician.Id, newDueDate));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var reassignedWorkOrder = await response.ReadWorkOrderAsync();
        reassignedWorkOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        reassignedWorkOrder.TechnicianId.ShouldBe(otherTechnician.Id);
        reassignedWorkOrder.TechnicianName.ShouldBe(otherTechnician.FullName);
        reassignedWorkOrder.TechnicianEmail.ShouldBe(otherTechnician.Email);
        reassignedWorkOrder.DueDate.ShouldBe(newDueDate);
        response.ETag().ShouldBe(await client.GetETagAsync(WorkOrderRequests.WorkOrderUri(workOrder.Id), TestContext.Current.CancellationToken));
        using var previousTechnicianClient = await CreateAuthenticatedClientAsync(technician);
        (await previousTechnicianClient.ListWorkOrdersAsync()).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Work_Order_Is_In_Progress()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
        var technician = await CreateUserAsync(Roles.Technician);
        var otherTechnician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        using var startResponse = await technicianClient.PostTransitionAsync(workOrder.Id, "start");
        startResponse.EnsureSuccessStatusCode();

        using var response = await PostReassignAsync(client, workOrder.Id, new ReassignTechnicianRequest(otherTechnician.Id));

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.NotReassignable.Code);
        (await client.GetWorkOrderAsync(workOrder.Id)).TechnicianId.ShouldBe(technician.Id);
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Target_Technician_Is_Deactivated()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
        var technician = await CreateUserAsync(Roles.Technician);
        var deactivatedTechnician = await CreateUserAsync(Roles.Technician);
        await DeactivateUserDirectlyAsync(deactivatedTechnician.Id);
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);

        using var response = await PostReassignAsync(client, workOrder.Id, new ReassignTechnicianRequest(deactivatedTechnician.Id));

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, WorkOrderErrors.TechnicianNotFound.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Changed_Due_Date_Is_In_Past()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
        var technician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);

        using var response = await PostReassignAsync(client, workOrder.Id, new ReassignTechnicianRequest(technician.Id, DateTimeOffset.UtcNow.AddHours(-1)));

        await response.ShouldBeValidationProblemAsync("dueDate");
    }

    private static Task<HttpResponseMessage> PostReassignAsync(HttpClient client, Guid workOrderId, ReassignTechnicianRequest request) =>
        client.PostAsJsonAsync(new Uri($"/api/v1/work-orders/{workOrderId}/reassign", UriKind.Relative), request, ApiJson.Options, TestContext.Current.CancellationToken);
}
