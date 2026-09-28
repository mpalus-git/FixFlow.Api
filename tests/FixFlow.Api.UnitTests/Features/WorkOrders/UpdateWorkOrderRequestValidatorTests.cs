using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;

namespace FixFlow.Api.UnitTests.Features.WorkOrders;

public sealed class UpdateWorkOrderRequestValidatorTests
{
    private readonly UpdateWorkOrderRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Work_Order_Details_Are_Valid()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2001)]
    public void Should_Reject_Request_When_Description_Is_Empty_Or_Longer_Than_Two_Thousand_Characters(int descriptionLength)
    {
        var result = _validator.Validate(ValidRequest() with { Description = new string('a', descriptionLength) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateWorkOrderRequest.Description));
    }

    [Fact]
    public void Should_Reject_Request_When_Due_Date_Is_Missing()
    {
        var result = _validator.Validate(ValidRequest() with { DueDate = default });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateWorkOrderRequest.DueDate));
    }

    [Fact]
    public void Should_Reject_Request_When_Priority_Is_Not_Defined()
    {
        var result = _validator.Validate(ValidRequest() with { Priority = (WorkOrderPriority)99 });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateWorkOrderRequest.Priority));
    }

    private static UpdateWorkOrderRequest ValidRequest() =>
        new("Leak and noisy fan", WorkOrderPriority.Critical, new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
}
