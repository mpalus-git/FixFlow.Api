namespace FixFlow.Api.Common.Time;

public static class BusinessTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

    public static DateTimeOffset From(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, Zone);

    public static DateTimeOffset StartOfDay(DateOnly date)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(localMidnight, Zone.GetUtcOffset(localMidnight)).ToUniversalTime();
    }

    public static DateOnly StartOfWeek(DateTimeOffset moment)
    {
        var day = DateOnly.FromDateTime(From(moment).DateTime);
        var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
        return day.AddDays(-daysSinceMonday);
    }
}
