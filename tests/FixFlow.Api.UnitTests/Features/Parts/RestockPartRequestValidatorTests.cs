using FixFlow.Api.Features.Parts.RestockPart;

namespace FixFlow.Api.UnitTests.Features.Parts;

public sealed class RestockPartRequestValidatorTests
{
    private readonly RestockPartRequestValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(RestockPartRequestValidator.MaxDeliveryQuantity)]
    public void Should_Accept_Request_When_Quantity_Is_Within_Allowed_Range(int quantity)
    {
        var result = _validator.Validate(new RestockPartRequest(quantity));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(RestockPartRequestValidator.MaxDeliveryQuantity + 1)]
    public void Should_Reject_Request_When_Quantity_Is_Outside_Allowed_Range(int quantity)
    {
        var result = _validator.Validate(new RestockPartRequest(quantity));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(RestockPartRequest.Quantity));
    }
}
