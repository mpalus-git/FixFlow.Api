using FixFlow.Api.Common.Pagination;
using FluentValidation;

namespace FixFlow.Api.Features.Devices.ListDevices;

public sealed class ListDevicesRequestValidator : AbstractValidator<ListDevicesRequest>
{
    public ListDevicesRequestValidator()
    {
        Include(new PagedRequestValidator());
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
