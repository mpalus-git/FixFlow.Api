using FixFlow.Api.Common.Pagination;
using FluentValidation;

namespace FixFlow.Api.Features.Devices.ListDeviceWorkOrders;

public sealed class ListDeviceWorkOrdersRequestValidator : AbstractValidator<ListDeviceWorkOrdersRequest>
{
    public ListDeviceWorkOrdersRequestValidator()
    {
        Include(new PagedRequestValidator());
    }
}
