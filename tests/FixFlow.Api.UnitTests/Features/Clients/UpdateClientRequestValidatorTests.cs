using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Clients.UpdateClient;

namespace FixFlow.Api.UnitTests.Features.Clients;

public sealed class UpdateClientRequestValidatorTests
{
    private static readonly ClientAddress ValidAddress = new("Marszałkowska", "10A", "00-590", "Warszawa");

    private readonly UpdateClientRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Client_Details_Are_Valid()
    {
        var result = _validator.Validate(new UpdateClientRequest("Klimat-Serwis", ValidAddress, "Anna Nowak", "600100200", "biuro@klimat.test"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Apply_Shared_Client_Rules_When_Address_And_Phone_Are_Invalid()
    {
        var request = new UpdateClientRequest("Klimat-Serwis", ValidAddress with { City = string.Empty }, "Anna Nowak", "123");

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(error => error.PropertyName == "Address.City");
        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateClientRequest.Phone));
    }
}
