using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.ListWorkOrders;

namespace FixFlow.Api.UnitTests.Features.WorkOrders;

public sealed class ListWorkOrdersRequestValidatorTests
{
    private readonly ListWorkOrdersRequestValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData(WorkOrderStatus.InProgress)]
    public void Should_Accept_Request_When_Status_Filter_Is_Omitted_Or_Defined(WorkOrderStatus? status)
    {
        var result = _validator.Validate(new ListWorkOrdersRequest(Status: status));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Status_Filter_Is_Not_Defined()
    {
        var result = _validator.Validate(new ListWorkOrdersRequest(Status: (WorkOrderStatus)99));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListWorkOrdersRequest.Status));
    }
}
