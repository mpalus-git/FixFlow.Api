using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.UnitTests.Domain.WorkOrders;

public sealed class WorkOrderNumberTests
{
    [Theory]
    [InlineData(2026, 1, "ZL/2026/0001")]
    [InlineData(2026, 42, "ZL/2026/0042")]
    [InlineData(2027, 9999, "ZL/2027/9999")]
    [InlineData(2027, 12345, "ZL/2027/12345")]
    public void Should_Format_Year_And_Sequence_With_At_Least_Four_Digits_When_Number_Is_Formatted(int year, int sequence, string expectedNumber)
    {
        WorkOrderNumber.Format(year, sequence).ShouldBe(expectedNumber);
    }
}
