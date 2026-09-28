using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class UpdateWorkOrderTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Replace_Details_And_Keep_Status_And_Technician_When_Dispatcher_Updates_Assigned_Work_Order()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);
        var technician = await CreateUserAsync(Roles.Technician);
        using var assignResponse = await client.PostAssignAsync(workOrder.Id, technician.Id);
        var request = new UpdateWorkOrderRequest("Leak and noisy fan", WorkOrderPriority.Critical, workOrder.DueDate.AddDays(2));

        using var response = await PutAsync(client, workOrder.Id, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var storedWorkOrder = await client.GetWorkOrderAsync(workOrder.Id);
        storedWorkOrder.Description.ShouldBe(request.Description);
        storedWorkOrder.Priority.ShouldBe(WorkOrderPriority.Critical);
        storedWorkOrder.DueDate.ShouldBe(request.DueDate);
        storedWorkOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        storedWorkOrder.TechnicianId.ShouldBe(technician.Id);
    }

    [Fact]
    public async Task Should_Accept_Update_When_Due_Date_Is_Sent_Back_Unchanged()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);

        using var response = await PutAsync(client, workOrder.Id, new UpdateWorkOrderRequest("Printer jams paper", WorkOrderPriority.Low, workOrder.DueDate));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadWorkOrderAsync()).DueDate.ShouldBe(workOrder.DueDate);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Changed_Due_Date_Is_In_The_Past()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);

        using var response = await PutAsync(client, workOrder.Id, new UpdateWorkOrderRequest("Printer jams paper", WorkOrderPriority.Low, DateTimeOffset.UtcNow.AddHours(-1)));

        await response.ShouldBeValidationProblemAsync("dueDate");
    }

    [Fact]
    public async Task Should_Return_Bad_Request_When_Priority_Is_Missing()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);
        var request = new { description = "Printer jams paper", dueDate = workOrder.DueDate };

        using var response = await client.PutAsJsonAsync(WorkOrderRequests.WorkOrderUri(workOrder.Id), request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.GetWorkOrderAsync(workOrder.Id)).Priority.ShouldBe(workOrder.Priority);
    }

    private static async Task<WorkOrderResponse> CreateWorkOrderAsync(HttpClient client) =>
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid workOrderId, UpdateWorkOrderRequest request) =>
        client.PutAsJsonAsync(WorkOrderRequests.WorkOrderUri(workOrderId), request, ApiJson.Options, TestContext.Current.CancellationToken);
}
