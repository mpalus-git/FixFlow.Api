using FixFlow.Api.Common.Time;

namespace FixFlow.Api.Features.WorkOrders.SendDailySummary;

public sealed record SummaryPeriod(DateOnly Day, DateTimeOffset Start, DateTimeOffset End)
{
    public static SummaryPeriod PreviousDay(DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(BusinessTime.From(now).DateTime);
        var previousDay = today.AddDays(-1);
        return new SummaryPeriod(previousDay, StartOf(previousDay), StartOf(today));
    }

    private static DateTimeOffset StartOf(DateOnly day)
    {
        var localMidnight = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(localMidnight, BusinessTime.Zone.GetUtcOffset(localMidnight)).ToUniversalTime();
    }
}
