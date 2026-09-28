using FixFlow.Api.Common.Pagination;

namespace FixFlow.Api.UnitTests.Common.Pagination;

public sealed class PagedRequestValidatorTests
{
    private readonly PagedRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Default_Paging_Is_Used()
    {
        var result = _validator.Validate(new TestPagedRequest(1, PagedRequest.DefaultPageSize));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Accept_Request_When_Page_Is_Max_Page_With_Max_Page_Size()
    {
        var result = _validator.Validate(new TestPagedRequest(PagedRequest.MaxPage, PagedRequest.MaxPageSize));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Request_When_Page_Is_Lower_Than_One(int page)
    {
        var result = _validator.Validate(new TestPagedRequest(page, PagedRequest.DefaultPageSize));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(IPagedRequest.Page));
    }

    [Theory]
    [InlineData(PagedRequest.MaxPage + 1)]
    [InlineData(int.MaxValue)]
    public void Should_Reject_Request_When_Page_Is_Greater_Than_Max_Page(int page)
    {
        var result = _validator.Validate(new TestPagedRequest(page, PagedRequest.MaxPageSize));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(IPagedRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PagedRequest.MaxPageSize + 1)]
    public void Should_Reject_Request_When_Page_Size_Is_Outside_Allowed_Range(int pageSize)
    {
        var result = _validator.Validate(new TestPagedRequest(1, pageSize));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(IPagedRequest.PageSize));
    }

    private sealed record TestPagedRequest(int Page, int PageSize) : IPagedRequest;
}
