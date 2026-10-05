using FixFlow.Api.Features.Devices.ListDeviceWorkOrders;

namespace FixFlow.Api.UnitTests.Features.Devices;

public sealed class ListDeviceWorkOrdersRequestValidatorTests
{
    private readonly ListDeviceWorkOrdersRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Default_Paging_Is_Used()
    {
        var result = _validator.Validate(new ListDeviceWorkOrdersRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Page_Is_Zero()
    {
        var result = _validator.Validate(new ListDeviceWorkOrdersRequest(Page: 0));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListDeviceWorkOrdersRequest.Page));
    }

    [Fact]
    public void Should_Reject_Request_When_Page_Size_Exceeds_One_Hundred()
    {
        var result = _validator.Validate(new ListDeviceWorkOrdersRequest(PageSize: 101));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListDeviceWorkOrdersRequest.PageSize));
    }
}
