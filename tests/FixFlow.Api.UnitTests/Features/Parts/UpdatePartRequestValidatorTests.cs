using FixFlow.Api.Features.Parts.UpdatePart;

namespace FixFlow.Api.UnitTests.Features.Parts;

public sealed class UpdatePartRequestValidatorTests
{
    private readonly UpdatePartRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Part_Details_Are_Valid()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Apply_Shared_Part_Rules_When_Name_Is_Empty_And_Unit_Price_Has_Three_Decimal_Places()
    {
        var result = _validator.Validate(ValidRequest() with { Name = string.Empty, UnitPrice = 59.505m });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdatePartRequest.Name));
        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdatePartRequest.UnitPrice));
    }

    private static UpdatePartRequest ValidRequest() => new("Filtr węglowy", "FLT-200", 59.50m);
}
