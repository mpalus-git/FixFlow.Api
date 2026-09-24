namespace FixFlow.Api.Common.Persistence;

public sealed class DatabasePrecisionTimeProvider(TimeProvider innerTimeProvider) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => innerTimeProvider.LocalTimeZone;

    public override long TimestampFrequency => innerTimeProvider.TimestampFrequency;

    public override DateTimeOffset GetUtcNow() => innerTimeProvider.GetUtcNow().ToDatabasePrecision();

    public override long GetTimestamp() => innerTimeProvider.GetTimestamp();
}
