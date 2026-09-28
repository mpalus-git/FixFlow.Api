using System.Net.Http.Json;
using FixFlow.Api.Common.Jobs;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.MarkOverdueWorkOrders;
using FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;
using FixFlow.Api.IntegrationTests.ServiceEntries;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Quartz;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class MarkOverdueWorkOrdersTests(FixFlowApiFactory factory) : ServiceEntryTestBase(factory)
{
    private static readonly DateTimeOffset AfterDefaultDueDate = DateTimeOffset.UtcNow.AddDays(4);

    [Fact]
    public async Task Should_Mark_New_Assigned_And_In_Progress_Work_Orders_Overdue_When_Due_Date_Passed()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var client = scenario.DispatcherClient;
        var newWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync("SN-NEW")));
        var assignedWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync("SN-ASSIGNED")));
        await Factory.AssignTechnicianDirectlyAsync(assignedWorkOrder.Id, (await CreateUserAsync(Roles.Technician)).Id);

        var markedCount = await MarkOverdueWorkOrdersAtAsync(AfterDefaultDueDate);

        markedCount.ShouldBe(3);
        foreach (var workOrderId in new[] { newWorkOrder.Id, assignedWorkOrder.Id, scenario.WorkOrder.Id })
        {
            (await client.GetWorkOrderAsync(workOrderId)).IsOverdue.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Should_Not_Mark_Work_Order_Overdue_When_Due_Date_Has_Not_Passed()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync(), dueInDays: 5));

        var markedCount = await MarkOverdueWorkOrdersAtAsync(AfterDefaultDueDate);

        markedCount.ShouldBe(0);
        (await client.GetWorkOrderAsync(workOrder.Id)).IsOverdue.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Not_Mark_Work_Order_Overdue_When_It_Is_Completed_Or_Invoiced(bool invoiced)
    {
        using var scenario = await CreateCompletedWorkOrderAsync();
        if (invoiced)
        {
            using var response = await scenario.DispatcherClient.PostTransitionAsync(scenario.WorkOrder.Id, "invoice");
            response.EnsureSuccessStatusCode();
        }

        var markedCount = await MarkOverdueWorkOrdersAtAsync(AfterDefaultDueDate);

        markedCount.ShouldBe(0);
        (await scenario.DispatcherClient.GetWorkOrderAsync(scenario.WorkOrder.Id)).IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Count_Work_Order_Again_When_It_Is_Already_Overdue()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
        await MarkOverdueWorkOrdersAtAsync(AfterDefaultDueDate);

        var markedCount = await MarkOverdueWorkOrdersAtAsync(AfterDefaultDueDate.AddHours(1));

        markedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_List_Only_Overdue_Work_Orders_When_Filtered_By_IsOverdue()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var overdueWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync("SN-1"), dueInDays: 1));
        var onTimeWorkOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync("SN-2"), dueInDays: 5));
        await MarkOverdueWorkOrdersAtAsync(DateTimeOffset.UtcNow.AddDays(2));

        var overduePage = await client.ListWorkOrdersAsync("?isOverdue=true");
        var onTimePage = await client.ListWorkOrdersAsync("?isOverdue=false");

        overduePage.Items.Select(item => item.Id).ShouldBe([overdueWorkOrder.Id]);
        onTimePage.Items.Select(item => item.Id).ShouldBe([onTimeWorkOrder.Id]);
    }

    [Fact]
    public async Task Should_Clear_Overdue_When_Due_Date_Is_Moved_To_Future()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
        await MarkOverdueWorkOrdersAtAsync(AfterDefaultDueDate);
        var update = new UpdateWorkOrderRequest(workOrder.Description, workOrder.Priority, DateTimeOffset.UtcNow.AddDays(10));

        using var response = await client.PutWithCurrentETagAsync(WorkOrderRequests.WorkOrderUri(workOrder.Id), update, ApiJson.Options, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        (await response.ReadWorkOrderAsync()).IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Clear_Overdue_When_Work_Order_Is_Completed()
    {
        using var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();
        await MarkOverdueWorkOrdersAtAsync(AfterDefaultDueDate);

        using var response = await scenario.TechnicianClient.PostTransitionAsync(scenario.WorkOrder.Id, "complete");

        response.EnsureSuccessStatusCode();
        var completedWorkOrder = await response.ReadWorkOrderAsync();
        completedWorkOrder.Status.ShouldBe(WorkOrderStatus.Completed);
        completedWorkOrder.IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Mark_Overdue_On_Startup_And_Schedule_Hourly_Run_When_Jobs_Are_Enabled()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));
        await MoveDueDateToPastAsync(workOrder.Id);

        await using var factoryWithJobs = Factory.WithWebHostBuilder(builder => builder.UseSetting(JobsExtensions.EnabledSettingKey, "true"));
        var scheduler = await factoryWithJobs.Services.GetRequiredService<ISchedulerFactory>().GetScheduler(TestContext.Current.CancellationToken);

        (await WaitUntilOverdueAsync(client, workOrder.Id)).ShouldBeTrue();
        var triggers = await scheduler.GetTriggersOfJob(MarkOverdueWorkOrdersJob.Key, TestContext.Current.CancellationToken);
        triggers.OfType<ICronTrigger>().Select(trigger => trigger.CronExpressionString).ShouldBe([MarkOverdueWorkOrdersJob.HourlyCronExpression]);
    }

    private static async Task<bool> WaitUntilOverdueAsync(HttpClient client, Guid workOrderId)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if ((await client.GetWorkOrderAsync(workOrderId)).IsOverdue)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
        }

        return false;
    }

    private async Task<int> MarkOverdueWorkOrdersAtAsync(DateTimeOffset now)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = new MarkOverdueWorkOrdersHandler(
            scope.ServiceProvider.GetRequiredService<FixFlowDbContext>(),
            new FakeTimeProvider(now),
            NullLogger<MarkOverdueWorkOrdersHandler>.Instance);
        return await handler.HandleAsync(TestContext.Current.CancellationToken);
    }

    private async Task MoveDueDateToPastAsync(Guid workOrderId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        await dbContext.WorkOrders
            .Where(workOrder => workOrder.Id == workOrderId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(workOrder => workOrder.DueDate, DateTimeOffset.UtcNow.AddHours(-1)),
                TestContext.Current.CancellationToken);
    }
}
