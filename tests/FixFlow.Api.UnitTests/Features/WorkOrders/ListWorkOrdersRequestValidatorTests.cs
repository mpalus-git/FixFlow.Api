using System.Globalization;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.ListWorkOrders;

namespace FixFlow.Api.UnitTests.Features.WorkOrders;

public sealed class ListWorkOrdersRequestValidatorTests
{
    private readonly ListWorkOrdersRequestValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData(WorkOrderStatus.InProgress)]
    public void Should_Accept_Request_When_Status_Filter_Is_Omitted_Or_Defined(WorkOrderStatus? status)
    {
        var result = _validator.Validate(new ListWorkOrdersRequest(Status: status));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Status_Filter_Is_Not_Defined()
    {
        var result = _validator.Validate(new ListWorkOrdersRequest(Status: (WorkOrderStatus)99));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListWorkOrdersRequest.Status));
    }

    [Theory]
    [InlineData("2026-10-01", "2026-10-01")]
    [InlineData("2026-10-01", "2026-10-31")]
    [InlineData(null, "2026-10-31")]
    [InlineData("2026-10-01", null)]
    public void Should_Accept_Request_When_Due_Date_Range_Is_Open_Or_Ordered(string? dueFrom, string? dueTo)
    {
        var result = _validator.Validate(new ListWorkOrdersRequest(DueFrom: ParseDate(dueFrom), DueTo: ParseDate(dueTo)));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Due_Date_Range_Ends_Before_It_Starts()
    {
        var result = _validator.Validate(new ListWorkOrdersRequest(DueFrom: new DateOnly(2026, 10, 2), DueTo: new DateOnly(2026, 10, 1)));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListWorkOrdersRequest.DueTo));
    }

    [Fact]
    public void Should_Reject_Request_When_Search_Is_Longer_Than_100_Characters()
    {
        var result = _validator.Validate(new ListWorkOrdersRequest(Search: new string('a', 101)));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListWorkOrdersRequest.Search));
    }

    private static DateOnly? ParseDate(string? date) =>
        date is null ? null : DateOnly.Parse(date, CultureInfo.InvariantCulture);
}
