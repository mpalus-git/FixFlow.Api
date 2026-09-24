using System.Globalization;
using FixFlow.Api.Features.Parts.CreatePart;

namespace FixFlow.Api.UnitTests.Features.Parts;

public sealed class CreatePartRequestValidatorTests
{
    private readonly CreatePartRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Price_Is_Zero_And_Stock_Is_Empty()
    {
        var result = _validator.Validate(ValidRequest() with { UnitPrice = 0m, StockQuantity = 0 });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Accept_Request_When_Unit_Price_Has_Trailing_Zeros()
    {
        var result = _validator.Validate(ValidRequest() with { UnitPrice = 49.9900m });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Name_And_Catalog_Number_Are_Empty()
    {
        var result = _validator.Validate(ValidRequest() with { Name = " ", CatalogNumber = string.Empty });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreatePartRequest.Name));
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreatePartRequest.CatalogNumber));
    }

    [Fact]
    public void Should_Reject_Request_When_Catalog_Number_Is_Longer_Than_Fifty_Characters()
    {
        var result = _validator.Validate(ValidRequest() with { CatalogNumber = new string('A', 51) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreatePartRequest.CatalogNumber));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("10.001")]
    [InlineData("10000000000")]
    public void Should_Reject_Request_When_Unit_Price_Is_Negative_Or_Does_Not_Fit_Money_Format(string unitPrice)
    {
        var result = _validator.Validate(ValidRequest() with { UnitPrice = decimal.Parse(unitPrice, CultureInfo.InvariantCulture) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreatePartRequest.UnitPrice));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(CreatePartRequestValidator.MaxInitialStockQuantity + 1)]
    public void Should_Reject_Request_When_Stock_Quantity_Is_Out_Of_Range(int stockQuantity)
    {
        var result = _validator.Validate(ValidRequest() with { StockQuantity = stockQuantity });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreatePartRequest.StockQuantity));
    }

    private static CreatePartRequest ValidRequest() => new("Filtr powietrza", "FLT-100", 12, 49.99m);
}
