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

    [Theory]
    [InlineData("2026-01-14T22:59:00Z", "2026-01-14")]
    [InlineData("2026-01-14T23:00:00Z", "2026-01-15")]
    [InlineData("2026-07-01T21:59:00Z", "2026-07-01")]
    [InlineData("2026-07-01T22:00:00Z", "2026-07-02")]
    public void Should_Return_Warsaw_Calendar_Day_When_Today_Is_Requested(string moment, string expectedDay)
    {
        var today = BusinessTime.Today(DateTimeOffset.Parse(moment, CultureInfo.InvariantCulture));

        today.ShouldBe(DateOnly.Parse(expectedDay, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("2026-01-14T10:00:00Z", "2026-01-12")]
    [InlineData("2026-01-12T00:00:00Z", "2026-01-12")]
    [InlineData("2026-01-18T22:30:00Z", "2026-01-12")]
    [InlineData("2026-01-18T23:30:00Z", "2026-01-19")]
    [InlineData("2026-07-05T21:30:00Z", "2026-06-29")]
    [InlineData("2026-07-05T22:30:00Z", "2026-07-06")]
    [InlineData("2026-03-29T21:30:00Z", "2026-03-23")]
    [InlineData("2026-03-29T22:30:00Z", "2026-03-30")]
    [InlineData("2026-10-25T22:30:00Z", "2026-10-19")]
    [InlineData("2026-10-25T23:30:00Z", "2026-10-26")]
    public void Should_Return_Warsaw_Monday_When_Start_Of_Week_Is_Requested(string moment, string expectedMonday)
    {
        var startOfWeek = BusinessTime.StartOfWeek(DateTimeOffset.Parse(moment, CultureInfo.InvariantCulture));

        startOfWeek.ShouldBe(DateOnly.Parse(expectedMonday, CultureInfo.InvariantCulture));
    }
}
