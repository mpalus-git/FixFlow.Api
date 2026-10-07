using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.CompleteWorkOrder;
using FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;
using FixFlow.Api.IntegrationTests.ServiceEntries;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class CompleteAndInvoiceWorkOrderTests(FixFlowApiFactory factory) : ServiceEntryTestBase(factory)
{
    [Fact]
    public async Task Should_Complete_Work_Order_When_Assigned_Technician_Completes_It_With_Service_Entry()
    {
        using var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();

        using var response = await scenario.TechnicianClient.PostTransitionAsync(scenario.WorkOrder.Id, "complete");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var completedWorkOrder = await response.ReadWorkOrderAsync();
        completedWorkOrder.Status.ShouldBe(WorkOrderStatus.Completed);
        completedWorkOrder.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Complete_Work_Order_When_Dispatcher_Completes_It_As_Fallback()
    {
        using var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();

        using var response = await scenario.DispatcherClient.PostTransitionAsync(scenario.WorkOrder.Id, "complete");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadWorkOrderAsync()).Status.ShouldBe(WorkOrderStatus.Completed);
    }

    [Fact]
    public async Task Should_Complete_Work_Order_At_Requested_Time_When_Completion_Was_Recorded_Offline()
    {
        using var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();
        var completedAt = DateTimeOffset.UtcNow.ToDatabasePrecision();

        using var response = await scenario.TechnicianClient.PostCompleteAsync(scenario.WorkOrder.Id, new CompleteWorkOrderRequest(completedAt));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadWorkOrderAsync()).CompletedAt.ShouldBe(completedAt);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Requested_Completion_Is_Before_End_Of_Last_Work()
    {
        using var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();

        using var response = await scenario.TechnicianClient.PostCompleteAsync(scenario.WorkOrder.Id, new CompleteWorkOrderRequest(scenario.WorkOrder.StartedAt));

        await response.ShouldBeValidationProblemAsync("completedAt");
        (await scenario.TechnicianClient.GetWorkOrderAsync(scenario.WorkOrder.Id)).Status.ShouldBe(WorkOrderStatus.InProgress);
    }

    [Fact]
    public async Task Should_Return_Current_State_When_Completion_Is_Retried_After_Invoicing()
    {
        using var scenario = await CreateCompletedWorkOrderAsync();
        using var invoiceResponse = await scenario.DispatcherClient.PostTransitionAsync(scenario.WorkOrder.Id, "invoice");

        using var response = await scenario.TechnicianClient.PostTransitionAsync(scenario.WorkOrder.Id, "complete");

        invoiceResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.ETag().ShouldBe(invoiceResponse.ETag());
        (await response.ReadWorkOrderAsync()).Status.ShouldBe(WorkOrderStatus.Invoiced);
    }

    [Fact]
    public async Task Should_Reject_Completing_WorkOrder_When_No_ServiceEntry()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();

        using var response = await scenario.TechnicianClient.PostTransitionAsync(scenario.WorkOrder.Id, "complete");

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.NoServiceEntries.Code);
        (await scenario.TechnicianClient.GetWorkOrderAsync(scenario.WorkOrder.Id)).Status.ShouldBe(WorkOrderStatus.InProgress);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Assigned_Work_Order_Is_Completed_Before_Start()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician);
        var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);

        using var response = await client.PostTransitionAsync(workOrder.Id, "complete");

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "WorkOrder.InvalidStatusTransition");
    }

    [Fact]
    public async Task Should_Reject_Service_Entry_And_Update_When_Work_Order_Is_Completed()
    {
        using var scenario = await CreateCompletedWorkOrderAsync();
        var update = new UpdateWorkOrderRequest("Leak and noisy fan", WorkOrderPriority.Low, scenario.WorkOrder.DueDate);

        using var entryResponse = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.WorkEntry(scenario.WorkOrder));
        using var updateResponse = await scenario.DispatcherClient.PutWithCurrentETagAsync(WorkOrderRequests.WorkOrderUri(scenario.WorkOrder.Id), update, ApiJson.Options, TestContext.Current.CancellationToken);

        await entryResponse.ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.NotInProgress.Code);
        await updateResponse.ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.Closed.Code);
    }

    [Fact]
    public async Task Should_Invoice_Work_Order_When_Dispatcher_Invoices_Completed_Work_Order()
    {
        using var scenario = await CreateCompletedWorkOrderAsync();

        using var response = await scenario.DispatcherClient.PostTransitionAsync(scenario.WorkOrder.Id, "invoice");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var invoicedWorkOrder = await response.ReadWorkOrderAsync();
        invoicedWorkOrder.Status.ShouldBe(WorkOrderStatus.Invoiced);
        invoicedWorkOrder.InvoicedAt.ShouldNotBeNull();
        invoicedWorkOrder.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Work_Order_In_Progress_Is_Invoiced()
    {
        using var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();

        using var response = await scenario.DispatcherClient.PostTransitionAsync(scenario.WorkOrder.Id, "invoice");

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "WorkOrder.InvalidStatusTransition");
    }
}
