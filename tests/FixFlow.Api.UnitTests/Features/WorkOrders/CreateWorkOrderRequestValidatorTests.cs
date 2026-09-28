using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.CreateWorkOrder;

namespace FixFlow.Api.UnitTests.Features.WorkOrders;

public sealed class CreateWorkOrderRequestValidatorTests
{
    private readonly CreateWorkOrderRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Work_Order_Details_Are_Valid()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Device_Id_Is_Empty()
    {
        var result = _validator.Validate(ValidRequest() with { DeviceId = Guid.Empty });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateWorkOrderRequest.DeviceId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2001)]
    public void Should_Reject_Request_When_Description_Is_Empty_Or_Longer_Than_Two_Thousand_Characters(int descriptionLength)
    {
        var result = _validator.Validate(ValidRequest() with { Description = new string('a', descriptionLength) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateWorkOrderRequest.Description));
    }

    [Fact]
    public void Should_Reject_Request_When_Due_Date_Is_Missing()
    {
        var result = _validator.Validate(ValidRequest() with { DueDate = default });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateWorkOrderRequest.DueDate));
    }

    [Fact]
    public void Should_Reject_Request_When_Priority_Is_Not_Defined()
    {
        var result = _validator.Validate(ValidRequest() with { Priority = (WorkOrderPriority)99 });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateWorkOrderRequest.Priority));
    }

    private static CreateWorkOrderRequest ValidRequest() =>
        new(Guid.CreateVersion7(), "Air conditioner is leaking", new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero), WorkOrderPriority.High);
}
