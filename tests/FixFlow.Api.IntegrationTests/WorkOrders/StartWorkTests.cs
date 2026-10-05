using System.Net;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using FixFlow.Api.Features.WorkOrders.StartWork;
using FixFlow.Api.IntegrationTests.ServiceEntries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class StartWorkTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Move_Work_Order_To_In_Progress_When_Assigned_Technician_Starts_Work()
    {
        var (workOrders, technician) = await CreateAssignedWorkOrdersAsync(1);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);

        using var response = await technicianClient.PostTransitionAsync(workOrders[0].Id, "start");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var startedWorkOrder = await response.ReadWorkOrderAsync();
        startedWorkOrder.Status.ShouldBe(WorkOrderStatus.InProgress);
        startedWorkOrder.StartedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Accept_Service_Entry_From_Requested_Start_Time_When_Work_Is_Started_With_Past_Time()
    {
        var (workOrders, technician) = await CreateAssignedWorkOrdersAsync(1);
        var now = DateTimeOffset.UtcNow;
        await MoveAssignmentBackAsync(workOrders[0].Id, now.AddHours(-3));
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        var startedAt = now.AddHours(-2).ToDatabasePrecision();

        using var startResponse = await technicianClient.PostStartAsync(workOrders[0].Id, new StartWorkRequest(startedAt));
        var startedWorkOrder = await startResponse.ReadWorkOrderAsync();
        using var entryResponse = await technicianClient.PostServiceEntryAsync(
            workOrders[0].Id,
            ServiceEntryRequests.WorkEntry(startedWorkOrder) with { WorkStartedAt = startedAt, WorkFinishedAt = startedAt.AddHours(1) });

        startResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        startedWorkOrder.StartedAt.ShouldBe(startedAt);
        entryResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Requested_Start_Is_In_Future_Beyond_Clock_Skew()
    {
        var (workOrders, technician) = await CreateAssignedWorkOrdersAsync(1);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);

        using var response = await technicianClient.PostStartAsync(workOrders[0].Id, new StartWorkRequest(DateTimeOffset.UtcNow.AddMinutes(10)));

        await response.ShouldBeValidationProblemAsync("startedAt");
        (await technicianClient.GetWorkOrderAsync(workOrders[0].Id)).Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Requested_Start_Is_Before_Assignment()
    {
        var (workOrders, technician) = await CreateAssignedWorkOrdersAsync(1);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);

        using var response = await technicianClient.PostStartAsync(workOrders[0].Id, new StartWorkRequest(DateTimeOffset.UtcNow.AddHours(-1)));

        await response.ShouldBeValidationProblemAsync("startedAt");
        (await technicianClient.GetWorkOrderAsync(workOrders[0].Id)).Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Fact]
    public async Task Should_Reject_Starting_Second_Work_Order_When_Technician_Already_Has_Work_In_Progress()
    {
        var (workOrders, technician) = await CreateAssignedWorkOrdersAsync(2);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        using var firstResponse = await technicianClient.PostTransitionAsync(workOrders[0].Id, "start");

        using var secondResponse = await technicianClient.PostTransitionAsync(workOrders[1].Id, "start");

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        await secondResponse.ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.TechnicianAlreadyHasWorkInProgress.Code);
        (await technicianClient.GetWorkOrderAsync(workOrders[1].Id)).Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Fact]
    public async Task Should_Start_Only_One_Work_Order_When_Technician_Starts_Two_Work_Orders_Concurrently()
    {
        var (workOrders, technician) = await CreateAssignedWorkOrdersAsync(2);
        using var firstClient = await CreateAuthenticatedClientAsync(technician);
        using var secondClient = await CreateAuthenticatedClientAsync(technician);

        var responses = await Task.WhenAll(
            firstClient.PostTransitionAsync(workOrders[0].Id, "start"),
            secondClient.PostTransitionAsync(workOrders[1].Id, "start"));

        try
        {
            responses.Count(response => response.StatusCode == HttpStatusCode.OK).ShouldBe(1);
            await responses.Single(response => response.StatusCode != HttpStatusCode.OK)
                .ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.TechnicianAlreadyHasWorkInProgress.Code);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Should_Reject_Second_Work_In_Progress_In_Database_When_Domain_Check_Is_Bypassed()
    {
        var (workOrders, technician) = await CreateAssignedWorkOrdersAsync(2);
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var workOrderIds = workOrders.Select(workOrder => workOrder.Id).ToList();
        foreach (var workOrder in await dbContext.WorkOrders.Where(item => workOrderIds.Contains(item.Id)).ToListAsync(TestContext.Current.CancellationToken))
        {
            workOrder.Start(technician.Id, technicianHasWorkInProgress: false, DateTimeOffset.UtcNow);
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(
            WorkOrderConfiguration.TechnicianInProgressIndexName,
            WorkOrderErrors.TechnicianAlreadyHasWorkInProgress,
            TestContext.Current.CancellationToken);

        saving.FirstError.ShouldBe(WorkOrderErrors.TechnicianAlreadyHasWorkInProgress);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Work_Order_Is_Already_In_Progress()
    {
        var (workOrders, technician) = await CreateAssignedWorkOrdersAsync(1);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        using var firstResponse = await technicianClient.PostTransitionAsync(workOrders[0].Id, "start");

        using var response = await technicianClient.PostTransitionAsync(workOrders[0].Id, "start");

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "WorkOrder.InvalidStatusTransition");
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Dispatcher_Unassigns_Work_In_Progress()
    {
        var (workOrders, technician) = await CreateAssignedWorkOrdersAsync(1);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        using var startResponse = await technicianClient.PostTransitionAsync(workOrders[0].Id, "start");
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await dispatcherClient.PostTransitionAsync(workOrders[0].Id, "unassign");

        startResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "WorkOrder.InvalidStatusTransition");
    }

    private async Task MoveAssignmentBackAsync(Guid workOrderId, DateTimeOffset assignedAt)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        await dbContext.WorkOrders
            .Where(workOrder => workOrder.Id == workOrderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(workOrder => workOrder.AssignedAt, assignedAt), TestContext.Current.CancellationToken);
    }

    private async Task<(IReadOnlyList<WorkOrderResponse> WorkOrders, TestUser Technician)> CreateAssignedWorkOrdersAsync(int count)
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await dispatcherClient.CreateServicedDeviceAsync();
        var technician = await CreateUserAsync(Roles.Technician);
        var workOrders = new List<WorkOrderResponse>();
        for (var index = 0; index < count; index++)
        {
            var workOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
            using var assignResponse = await dispatcherClient.PostAssignAsync(workOrder.Id, technician.Id);
            workOrders.Add(await assignResponse.ReadWorkOrderAsync());
        }

        return (workOrders, technician);
    }
}
