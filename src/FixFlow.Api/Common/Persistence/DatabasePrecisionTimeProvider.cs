namespace FixFlow.Api.Common.Persistence;

public sealed class DatabasePrecisionTimeProvider(TimeProvider innerTimeProvider) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => innerTimeProvider.LocalTimeZone;

    public override long TimestampFrequency => innerTimeProvider.TimestampFrequency;

    public override DateTimeOffset GetUtcNow()
    {
        var now = innerTimeProvider.GetUtcNow();
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }

    public override long GetTimestamp() => innerTimeProvider.GetTimestamp();
}
