namespace FixFlow.Api.Features.WorkOrders.SendDailySummary;

public sealed record SummaryPeriod(DateOnly Day, DateTimeOffset Start, DateTimeOffset End)
{
    public static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

    public static SummaryPeriod PreviousDay(DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(ToBusinessTime(now).DateTime);
        var previousDay = today.AddDays(-1);
        return new SummaryPeriod(previousDay, StartOf(previousDay), StartOf(today));
    }

    public static DateTimeOffset ToBusinessTime(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, BusinessTimeZone);

    private static DateTimeOffset StartOf(DateOnly day)
    {
        var localMidnight = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(localMidnight, BusinessTimeZone.GetUtcOffset(localMidnight)).ToUniversalTime();
    }
}
