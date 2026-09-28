using FixFlow.Api.Features.Clients.ListClients;

namespace FixFlow.Api.UnitTests.Features.Clients;

public sealed class ListClientsRequestValidatorTests
{
    private readonly ListClientsRequestValidator _validator = new();

    [Fact]
    public void Should_Reject_Request_When_Search_Is_Longer_Than_One_Hundred_Characters()
    {
        var result = _validator.Validate(new ListClientsRequest(Search: new string('a', 101)));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListClientsRequest.Search));
    }
}
