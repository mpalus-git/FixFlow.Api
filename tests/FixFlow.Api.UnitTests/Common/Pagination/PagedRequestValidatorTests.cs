using FixFlow.Api.Common.Pagination;

namespace FixFlow.Api.UnitTests.Common.Pagination;

public sealed class PagedRequestValidatorTests
{
    private readonly PagedRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Page_Is_Max_Page_With_Max_Page_Size()
    {
        var result = _validator.Validate(new TestPagedRequest(PagedRequest.MaxPage, PagedRequest.MaxPageSize));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(PagedRequest.MaxPage + 1)]
    [InlineData(int.MaxValue)]
    public void Should_Reject_Request_When_Page_Is_Greater_Than_Max_Page(int page)
    {
        var result = _validator.Validate(new TestPagedRequest(page, PagedRequest.MaxPageSize));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(IPagedRequest.Page));
    }

    private sealed record TestPagedRequest(int Page, int PageSize) : IPagedRequest;
}
