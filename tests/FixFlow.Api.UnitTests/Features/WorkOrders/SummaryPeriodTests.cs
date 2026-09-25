using FixFlow.Api.Features.WorkOrders.SendDailySummary;

namespace FixFlow.Api.UnitTests.Features.WorkOrders;

public sealed class SummaryPeriodTests
{
    [Fact]
    public void Should_Cover_Previous_Warsaw_Calendar_Day_When_Summary_Is_Sent_In_Summer()
    {
        var period = SummaryPeriod.PreviousDay(new DateTimeOffset(2026, 9, 25, 5, 0, 0, TimeSpan.Zero));

        period.Day.ShouldBe(new DateOnly(2026, 9, 24));
        period.Start.ShouldBe(new DateTimeOffset(2026, 9, 23, 22, 0, 0, TimeSpan.Zero));
        period.End.ShouldBe(new DateTimeOffset(2026, 9, 24, 22, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Should_Use_Warsaw_Date_When_Utc_Date_Is_Still_Previous_Day()
    {
        var period = SummaryPeriod.PreviousDay(new DateTimeOffset(2026, 9, 24, 22, 30, 0, TimeSpan.Zero));

        period.Day.ShouldBe(new DateOnly(2026, 9, 24));
    }

    [Fact]
    public void Should_Cover_23_Hours_When_Previous_Day_Was_Spring_Forward_Day()
    {
        var period = SummaryPeriod.PreviousDay(new DateTimeOffset(2026, 3, 30, 5, 0, 0, TimeSpan.Zero));

        period.Day.ShouldBe(new DateOnly(2026, 3, 29));
        period.Start.ShouldBe(new DateTimeOffset(2026, 3, 28, 23, 0, 0, TimeSpan.Zero));
        (period.End - period.Start).ShouldBe(TimeSpan.FromHours(23));
    }

    [Fact]
    public void Should_Cover_25_Hours_When_Previous_Day_Was_Fall_Back_Day()
    {
        var period = SummaryPeriod.PreviousDay(new DateTimeOffset(2026, 10, 26, 6, 0, 0, TimeSpan.Zero));

        period.Day.ShouldBe(new DateOnly(2026, 10, 25));
        period.Start.ShouldBe(new DateTimeOffset(2026, 10, 24, 22, 0, 0, TimeSpan.Zero));
        (period.End - period.Start).ShouldBe(TimeSpan.FromHours(25));
    }
}
