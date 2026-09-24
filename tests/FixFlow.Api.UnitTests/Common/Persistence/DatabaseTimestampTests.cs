using FixFlow.Api.Common.Persistence;

namespace FixFlow.Api.UnitTests.Common.Persistence;

public sealed class DatabaseTimestampTests
{
    [Fact]
    public void Should_Convert_To_Utc_And_Truncate_To_Microseconds_When_Value_Has_Offset_And_Sub_Microsecond_Ticks()
    {
        var value = new DateTimeOffset(2026, 9, 27, 10, 0, 0, TimeSpan.FromHours(2)).AddTicks(1_234_567);

        var normalized = value.ToDatabasePrecision();

        normalized.Offset.ShouldBe(TimeSpan.Zero);
        normalized.ShouldBe(new DateTimeOffset(2026, 9, 27, 8, 0, 0, TimeSpan.Zero).AddTicks(1_234_560));
    }
}
