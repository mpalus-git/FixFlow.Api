using FixFlow.Api.Common.Email;
using FixFlow.Api.Common.Jobs;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.WorkOrders.SendDailySummary;
using FixFlow.Api.IntegrationTests.ServiceEntries;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Quartz;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class SendDailySummaryTests(FixFlowApiFactory factory) : ServiceEntryTestBase(factory)
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    [Fact]
    public async Task Should_Send_Summary_Only_To_Dispatchers_And_Admins()
    {
        var dispatcher = await CreateUserAsync(Roles.Dispatcher);
        var admin = await CreateUserAsync(Roles.Admin);
        await CreateUserAsync(Roles.Technician);

        await SendDailySummaryAtAsync(DateTimeOffset.UtcNow);

        var expectedRecipients = new[] { admin.Email, dispatcher.Email }.Order(StringComparer.Ordinal);
        SentMessage().Recipients.ShouldBe(expectedRecipients);
    }

    [Fact]
    public async Task Should_Report_Status_Counts_Overdue_And_Yesterday_Completed_Work_Orders()
    {
        using var scenario = await CreateCompletedWorkOrderAsync();
        var client = scenario.DispatcherClient;
        var overdueRequest = WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync("SN-OVERDUE")) with { DueDate = DateTimeOffset.UtcNow.AddHours(1) };
        await client.CreateWorkOrderAsync(overdueRequest);
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync("SN-ON-TIME"), dueInDays: 5));
        var completedAt = (await client.GetWorkOrderAsync(scenario.WorkOrder.Id)).CompletedAt.ShouldNotBeNull();

        await SendDailySummaryAtAsync(SevenAmInWarsawAfter(completedAt, days: 1));

        var body = SentMessage().Body;
        body.ShouldContain("- New: 2");
        body.ShouldContain("- Assigned: 0");
        body.ShouldContain("- In progress: 0");
        body.ShouldContain("- Completed, awaiting invoice: 1");
        body.ShouldContain("Overdue work orders (1)");
        body.ShouldContain("- SN-OVERDUE | High | due ");
        body.ShouldContain("| no technician");
        body.ShouldContain("Completed on ");
        body.ShouldContain("- AC-1001 | completed ");
        body.ShouldContain("| technician technician-");
    }

    [Fact]
    public async Task Should_Not_Report_Work_Order_As_Completed_Yesterday_When_It_Was_Completed_Earlier()
    {
        using var scenario = await CreateCompletedWorkOrderAsync();
        var completedAt = (await scenario.DispatcherClient.GetWorkOrderAsync(scenario.WorkOrder.Id)).CompletedAt.ShouldNotBeNull();

        await SendDailySummaryAtAsync(SevenAmInWarsawAfter(completedAt, days: 2));

        var body = SentMessage().Body;
        body.ShouldContain("- Completed, awaiting invoice: 1");
        body.ShouldContain("No work orders were completed.");
    }

    [Fact]
    public async Task Should_Send_Summary_When_There_Is_Nothing_To_Report()
    {
        await CreateUserAsync(Roles.Dispatcher);

        await SendDailySummaryAtAsync(DateTimeOffset.UtcNow);

        var body = SentMessage().Body;
        body.ShouldContain("- New: 0");
        body.ShouldContain("No overdue work orders.");
        body.ShouldContain("No work orders were completed.");
    }

    [Fact]
    public async Task Should_Not_Send_Summary_When_There_Are_No_Dispatchers_Or_Admins()
    {
        await CreateUserAsync(Roles.Technician);

        await SendDailySummaryAtAsync(DateTimeOffset.UtcNow);

        _emailSender.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Schedule_Daily_Summary_At_Seven_Warsaw_Time_When_Jobs_Are_Enabled()
    {
        await using var factoryWithJobs = Factory.WithWebHostBuilder(builder => builder.UseSetting(JobsExtensions.EnabledSettingKey, "true"));
        var scheduler = await factoryWithJobs.Services.GetRequiredService<ISchedulerFactory>().GetScheduler(TestContext.Current.CancellationToken);

        var triggers = await scheduler.GetTriggersOfJob(SendDailySummaryJob.Key, TestContext.Current.CancellationToken);

        var trigger = triggers.ShouldHaveSingleItem().ShouldBeAssignableTo<ICronTrigger>().ShouldNotBeNull();
        trigger.CronExpressionString.ShouldBe(SendDailySummaryJob.DailyCronExpression);
        trigger.TimeZone.Id.ShouldBe(SummaryPeriod.BusinessTimeZone.Id);
    }

    private static DateTimeOffset SevenAmInWarsawAfter(DateTimeOffset moment, int days)
    {
        var localDay = DateOnly.FromDateTime(SummaryPeriod.ToBusinessTime(moment).DateTime).AddDays(days);
        var localSevenAm = localDay.ToDateTime(new TimeOnly(7, 0));
        return new DateTimeOffset(localSevenAm, SummaryPeriod.BusinessTimeZone.GetUtcOffset(localSevenAm)).ToUniversalTime();
    }

    private async Task SendDailySummaryAtAsync(DateTimeOffset now)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = new SendDailySummaryHandler(
            scope.ServiceProvider.GetRequiredService<FixFlowDbContext>(),
            _emailSender,
            new FakeTimeProvider(now),
            NullLogger<SendDailySummaryHandler>.Instance);
        await handler.HandleAsync(TestContext.Current.CancellationToken);
    }

    private EmailMessage SentMessage() =>
        _emailSender.ReceivedCalls().ShouldHaveSingleItem().GetArguments()[0].ShouldBeOfType<EmailMessage>();
}
