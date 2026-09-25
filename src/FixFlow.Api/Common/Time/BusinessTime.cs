namespace FixFlow.Api.Common.Time;

public static class BusinessTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

    public static DateTimeOffset From(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, Zone);
}
