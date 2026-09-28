using System.Net;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
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
    public async Task Should_Return_Not_Found_Problem_When_Technician_Starts_Work_Order_Of_Another_Technician()
    {
        var (workOrders, _) = await CreateAssignedWorkOrdersAsync(1);
        using var otherTechnicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await otherTechnicianClient.PostTransitionAsync(workOrders[0].Id, "start");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, WorkOrderErrors.NotFound.Code);
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
