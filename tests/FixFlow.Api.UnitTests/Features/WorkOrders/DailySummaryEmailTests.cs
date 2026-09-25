using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.SendDailySummary;

namespace FixFlow.Api.UnitTests.Features.WorkOrders;

public sealed class DailySummaryEmailTests
{
    private static readonly DateOnly Day = new(2026, 9, 24);
    private static readonly string[] Recipients = ["admin@fixflow.test", "dispatcher@fixflow.test"];

    [Fact]
    public void Should_Describe_Counts_Overdue_And_Completed_Work_Orders_In_Warsaw_Time()
    {
        var summary = new DailySummary(
            Day,
            [new(WorkOrderStatus.New, 2), new(WorkOrderStatus.InProgress, 1), new(WorkOrderStatus.Completed, 3)],
            [new("AC-1001", WorkOrderPriority.Critical, new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero), "technician@fixflow.test")],
            [new("AC-2002", new DateTimeOffset(2026, 9, 24, 14, 30, 0, TimeSpan.Zero), null)]);

        var message = DailySummaryEmail.Create(summary, Recipients);

        message.Recipients.ShouldBe(Recipients);
        message.Subject.ShouldBe("FixFlow daily summary for 2026-09-24");
        message.Body.ShouldContain("- New: 2");
        message.Body.ShouldContain("- In progress: 1");
        message.Body.ShouldContain("- Completed, awaiting invoice: 3");
        message.Body.ShouldContain("Overdue work orders (1)");
        message.Body.ShouldContain("- AC-1001 | Critical | due 2026-09-23 14:00 | technician technician@fixflow.test");
        message.Body.ShouldContain("Completed on 2026-09-24 (1)");
        message.Body.ShouldContain("- AC-2002 | completed 2026-09-24 16:30 | no technician");
    }

    [Fact]
    public void Should_State_That_Sections_Are_Empty_When_Nothing_Is_Overdue_Or_Completed()
    {
        var summary = new DailySummary(Day, [new(WorkOrderStatus.New, 0)], [], []);

        var message = DailySummaryEmail.Create(summary, Recipients);

        message.Body.ShouldContain("Overdue work orders (0)");
        message.Body.ShouldContain("No overdue work orders.");
        message.Body.ShouldContain("Completed on 2026-09-24 (0)");
        message.Body.ShouldContain("No work orders were completed.");
    }
}
