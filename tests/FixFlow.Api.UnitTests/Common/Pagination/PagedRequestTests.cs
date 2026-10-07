using FixFlow.Api.Common.Pagination;

namespace FixFlow.Api.UnitTests.Common.Pagination;

public sealed class PagedRequestTests
{
    [Theory]
    [InlineData(1, 20, 7, 7)]
    [InlineData(1, 20, 0, 0)]
    [InlineData(3, 2, 1, 5)]
    [InlineData(PagedRequest.MaxPage, PagedRequest.MaxPageSize, 99, 100_000_000 - 1)]
    public void Should_Infer_Total_Count_When_Page_Is_Not_Full(int page, int pageSize, int itemCount, int expectedTotalCount)
    {
        PagedRequest.InferTotalCount(page, pageSize, itemCount).ShouldBe(expectedTotalCount);
    }

    [Fact]
    public void Should_Not_Infer_Total_Count_When_Page_Is_Full()
    {
        PagedRequest.InferTotalCount(2, 20, 20).ShouldBeNull();
    }

    [Fact]
    public void Should_Not_Infer_Total_Count_When_Page_Beyond_First_Is_Empty()
    {
        PagedRequest.InferTotalCount(4, 2, 0).ShouldBeNull();
    }
}
