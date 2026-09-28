using System.Globalization;
using FixFlow.Api.Common.Time;

namespace FixFlow.Api.UnitTests.Common.Time;

public sealed class BusinessTimeTests
{
    [Theory]
    [InlineData("2026-01-15", "2026-01-14T23:00:00Z")]
    [InlineData("2026-07-01", "2026-06-30T22:00:00Z")]
    [InlineData("2026-03-29", "2026-03-28T23:00:00Z")]
    [InlineData("2026-03-30", "2026-03-29T22:00:00Z")]
    [InlineData("2026-10-25", "2026-10-24T22:00:00Z")]
    [InlineData("2026-10-26", "2026-10-25T23:00:00Z")]
    public void Should_Return_Utc_Moment_Of_Warsaw_Midnight_When_Start_Of_Day_Is_Requested(string date, string expectedUtc)
    {
        var startOfDay = BusinessTime.StartOfDay(DateOnly.Parse(date, CultureInfo.InvariantCulture));

        startOfDay.ShouldBe(DateTimeOffset.Parse(expectedUtc, CultureInfo.InvariantCulture));
        startOfDay.Offset.ShouldBe(TimeSpan.Zero);
    }
}
