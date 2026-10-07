using FixFlow.Api.Common.Time;

namespace FixFlow.Api.Features.WorkOrders.SendDailySummary;

public sealed record SummaryPeriod(DateOnly Day, DateTimeOffset Start, DateTimeOffset End)
{
    public static SummaryPeriod PreviousDay(DateTimeOffset now)
    {
        var today = BusinessTime.Today(now);
        var previousDay = today.AddDays(-1);
        return new SummaryPeriod(previousDay, BusinessTime.StartOfDay(previousDay), BusinessTime.StartOfDay(today));
    }
}
