using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.Dashboard.GetDashboardSummary;
using FixFlow.Api.Features.WorkOrders.MarkOverdueWorkOrders;
using FixFlow.Api.IntegrationTests.WorkOrders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FixFlow.Api.IntegrationTests.Dashboard;

public sealed class GetDashboardSummaryTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Uri DashboardSummaryUri = new("/api/v1/dashboard/summary", UriKind.Relative);

    private static readonly WorkOrderStatus[] LifecycleStatuses =
    [
        WorkOrderStatus.New,
        WorkOrderStatus.Assigned,
        WorkOrderStatus.InProgress,
        WorkOrderStatus.Completed,
        WorkOrderStatus.Invoiced,
    ];

    [Theory]
    [InlineData(Roles.Dispatcher)]
    [InlineData(Roles.Admin)]
    public async Task Should_Return_All_Statuses_With_Zeros_When_There_Are_No_Work_Orders(string role)
    {
        using var client = await CreateAuthenticatedClientAsync(role);
        var requestedAt = DateTimeOffset.UtcNow;

        var summary = await GetSummaryAsync(client);

        summary.StatusCounts.Select(item => item.Status).ShouldBe(LifecycleStatuses);
        summary.StatusCounts.ShouldAllBe(item => item.Count == 0);
        summary.OverdueCount.ShouldBe(0);
        summary.Technicians.ShouldBeEmpty();
        summary.GeneratedAt.ShouldBeGreaterThanOrEqualTo(requestedAt.AddSeconds(-1));
        summary.WeekStart.ShouldBe(BusinessTime.StartOfWeek(summary.GeneratedAt));
        summary.WeekEnd.ShouldBe(summary.WeekStart.AddDays(6));
    }

    [Fact]
    public async Task Should_Return_Unauthorized_When_Request_Has_No_Token()
    {
        using var client = Factory.CreateClient();

        using var response = await client.GetAsync(DashboardSummaryUri, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Count_Work_Orders_By_Status_When_Work_Orders_Exist()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician);
        var deviceId = await client.CreateServicedDeviceAsync();
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        var assigned = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        await Factory.AssignTechnicianDirectlyAsync(assigned.Id, technician.Id);

        var summary = await GetSummaryAsync(client);

        summary.StatusCounts.ShouldBe(
        [
            new WorkOrderStatusCountResponse(WorkOrderStatus.New, 2),
            new WorkOrderStatusCountResponse(WorkOrderStatus.Assigned, 1),
            new WorkOrderStatusCountResponse(WorkOrderStatus.InProgress, 0),
            new WorkOrderStatusCountResponse(WorkOrderStatus.Completed, 0),
            new WorkOrderStatusCountResponse(WorkOrderStatus.Invoiced, 0),
        ]);
    }

    [Fact]
    public async Task Should_Match_Overdue_Filter_Of_Work_Order_List_When_Work_Orders_Are_Flagged_As_Overdue()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId) with { DueDate = DateTimeOffset.UtcNow.AddHours(1) });
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId, dueInDays: 5));
        await MarkOverdueWorkOrdersAtAsync(DateTimeOffset.UtcNow.AddHours(2));

        var summary = await GetSummaryAsync(client);

        var overdueList = await client.ListWorkOrdersAsync("?isOverdue=true");
        summary.OverdueCount.ShouldBe(1);
        summary.OverdueCount.ShouldBe(overdueList.TotalCount);
    }

    [Fact]
    public async Task Should_List_Only_Active_Technicians_Sorted_By_Full_Name_When_Summary_Is_Requested()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await CreateUserAsync(Roles.Admin);
        var technicianNamedZofia = await CreateUserAsync(Roles.Technician, "Zofia Adamska");
        var technicianNamedAdam = await CreateUserAsync(Roles.Technician, "Adam Zawadzki");
        var deactivatedTechnician = await CreateUserAsync(Roles.Technician);
        await DeactivateUserDirectlyAsync(deactivatedTechnician.Id);

        var summary = await GetSummaryAsync(client);

        var expectedTechnicians = new[] { technicianNamedAdam, technicianNamedZofia }
            .Select(technician => new TechnicianWorkloadResponse(technician.Id, technician.Email, technician.FullName, 0, 0, 0, 0));
        summary.Technicians.ShouldBe(expectedTechnicians);
    }

    [Fact]
    public async Task Should_Count_Technician_Workload_When_Technician_Has_Open_Work_Orders()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        var deviceId = await client.CreateServicedDeviceAsync();
        var overdue = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId) with { DueDate = DateTimeOffset.UtcNow.AddHours(1) });
        var started = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId, dueInDays: 5));
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId, dueInDays: 5));
        await Factory.AssignTechnicianDirectlyAsync(overdue.Id, technician.Id);
        await Factory.AssignTechnicianDirectlyAsync(started.Id, technician.Id);
        using var startResponse = await technicianClient.PostTransitionAsync(started.Id, "start");
        startResponse.EnsureSuccessStatusCode();
        await MarkOverdueWorkOrdersAtAsync(DateTimeOffset.UtcNow.AddHours(2));

        var summary = await GetSummaryAsync(client);

        var workload = summary.Technicians.ShouldHaveSingleItem();
        workload.TechnicianId.ShouldBe(technician.Id);
        workload.AssignedCount.ShouldBe(1);
        workload.InProgressCount.ShouldBe(1);
        workload.OverdueCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Count_Work_Orders_Due_This_Week_When_Due_Dates_Are_Near_Warsaw_Week_Bounds()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician);
        var deviceId = await client.CreateServicedDeviceAsync();
        var monday = BusinessTime.StartOfWeek(DateTimeOffset.UtcNow).AddDays(14);
        var sunday = monday.AddDays(6);
        DateTimeOffset[] dueDates =
        [
            WarsawTime(monday.AddDays(-1), 23, 30),
            WarsawTime(monday, 0, 30),
            WarsawTime(sunday, 23, 30),
            WarsawTime(sunday.AddDays(1), 0, 30),
        ];
        foreach (var dueDate in dueDates)
        {
            var workOrder = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId) with { DueDate = dueDate });
            await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);
        }

        var summary = await GetSummaryAtAsync(WarsawTime(monday.AddDays(2), 12, 0));

        summary.WeekStart.ShouldBe(monday);
        summary.WeekEnd.ShouldBe(sunday);
        var workload = summary.Technicians.ShouldHaveSingleItem();
        workload.AssignedCount.ShouldBe(4);
        workload.DueThisWeekCount.ShouldBe(2);
    }

    private static DateTimeOffset WarsawTime(DateOnly day, int hour, int minute)
    {
        var localTime = day.ToDateTime(new TimeOnly(hour, minute));
        return new DateTimeOffset(localTime, BusinessTime.Zone.GetUtcOffset(localTime)).ToUniversalTime();
    }

    private static async Task<DashboardSummaryResponse> GetSummaryAsync(HttpClient client)
    {
        var summary = await client.GetFromJsonAsync<DashboardSummaryResponse>(DashboardSummaryUri, ApiJson.Options, TestContext.Current.CancellationToken);
        return summary.ShouldNotBeNull();
    }

    private async Task<DashboardSummaryResponse> GetSummaryAtAsync(DateTimeOffset now)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = new GetDashboardSummaryHandler(scope.ServiceProvider.GetRequiredService<FixFlowDbContext>(), new FakeTimeProvider(now));
        return await handler.HandleAsync(TestContext.Current.CancellationToken);
    }

    private async Task MarkOverdueWorkOrdersAtAsync(DateTimeOffset now)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = new MarkOverdueWorkOrdersHandler(
            scope.ServiceProvider.GetRequiredService<FixFlowDbContext>(),
            new FakeTimeProvider(now),
            NullLogger<MarkOverdueWorkOrdersHandler>.Instance);
        await handler.HandleAsync(TestContext.Current.CancellationToken);
    }
}
