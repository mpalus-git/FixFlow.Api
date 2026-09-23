using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Clients.CreateClient;

namespace FixFlow.Api.UnitTests.Features.Clients;

public sealed class CreateClientRequestValidatorTests
{
    private static readonly ClientAddress ValidAddress = new("Marszałkowska", "10A", "00-590", "Warszawa");

    private readonly CreateClientRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_All_Required_Fields_Are_Valid_And_Email_Is_Missing()
    {
        var result = _validator.Validate(new CreateClientRequest("Klimat-Serwis", ValidAddress, "Anna Nowak", "+48 600-100-200"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("00590")]
    [InlineData("00-59")]
    [InlineData("AB-123")]
    public void Should_Reject_Request_When_Postal_Code_Has_Wrong_Format(string postalCode)
    {
        var request = new CreateClientRequest("Klimat-Serwis", ValidAddress with { PostalCode = postalCode }, "Anna Nowak", "600100200");

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(error => error.PropertyName == "Address.PostalCode");
    }

    [Fact]
    public void Should_Reject_Request_When_Building_Number_Is_Empty()
    {
        var request = new CreateClientRequest("Klimat-Serwis", ValidAddress with { BuildingNumber = string.Empty }, "Anna Nowak", "600100200");

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(error => error.PropertyName == "Address.BuildingNumber");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("phone-number")]
    public void Should_Reject_Request_When_Phone_Is_Empty_Or_Invalid(string phone)
    {
        var result = _validator.Validate(new CreateClientRequest("Klimat-Serwis", ValidAddress, "Anna Nowak", phone));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateClientRequest.Phone));
    }

    [Fact]
    public void Should_Reject_Request_When_Email_Is_Provided_But_Invalid()
    {
        var result = _validator.Validate(new CreateClientRequest("Klimat-Serwis", ValidAddress, "Anna Nowak", "600100200", "not-an-email"));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateClientRequest.Email));
    }

    [Fact]
    public void Should_Reject_Request_When_Name_And_Contact_Person_Are_Empty()
    {
        var result = _validator.Validate(new CreateClientRequest(string.Empty, ValidAddress, string.Empty, "600100200"));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateClientRequest.Name));
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateClientRequest.ContactPerson));
    }
}
