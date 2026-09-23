using FixFlow.Api.Features.Clients.ListClients;

namespace FixFlow.Api.UnitTests.Features.Clients;

public sealed class ListClientsRequestValidatorTests
{
    private readonly ListClientsRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Default_Paging_Is_Used()
    {
        var result = _validator.Validate(new ListClientsRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Request_When_Page_Is_Lower_Than_One(int page)
    {
        var result = _validator.Validate(new ListClientsRequest(Page: page));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListClientsRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Should_Reject_Request_When_Page_Size_Is_Outside_Allowed_Range(int pageSize)
    {
        var result = _validator.Validate(new ListClientsRequest(PageSize: pageSize));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListClientsRequest.PageSize));
    }

    [Fact]
    public void Should_Reject_Request_When_Search_Is_Longer_Than_One_Hundred_Characters()
    {
        var result = _validator.Validate(new ListClientsRequest(Search: new string('a', 101)));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListClientsRequest.Search));
    }
}
