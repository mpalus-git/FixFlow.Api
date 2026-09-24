namespace FixFlow.Api.Common.Persistence;

public static class DatabaseTimestamp
{
    public static DateTimeOffset ToDatabasePrecision(this DateTimeOffset value)
    {
        var utcValue = value.ToUniversalTime();
        return utcValue.AddTicks(-(utcValue.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
