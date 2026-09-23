using FixFlow.Api.Common.Persistence;
using Microsoft.Extensions.Time.Testing;

namespace FixFlow.Api.UnitTests.Common.Persistence;

public sealed class DatabasePrecisionTimeProviderTests
{
    [Fact]
    public void Should_Truncate_Current_Time_To_Microseconds_When_Inner_Time_Has_Ticks_Below_Microsecond()
    {
        var innerTime = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero).AddTicks(1_234_567);
        var timeProvider = new DatabasePrecisionTimeProvider(new FakeTimeProvider(innerTime));

        var now = timeProvider.GetUtcNow();

        now.ShouldBe(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero).AddTicks(1_234_560));
        now.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Should_Return_Same_Time_When_Inner_Time_Is_Already_Whole_Microseconds()
    {
        var innerTime = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero).AddMicroseconds(250);
        var timeProvider = new DatabasePrecisionTimeProvider(new FakeTimeProvider(innerTime));

        timeProvider.GetUtcNow().ShouldBe(innerTime);
    }
}
