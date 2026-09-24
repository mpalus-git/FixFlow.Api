using FixFlow.Api.Common.Pagination;
using FluentValidation;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrders;

public sealed class ListWorkOrdersRequestValidator : AbstractValidator<ListWorkOrdersRequest>
{
    public ListWorkOrdersRequestValidator()
    {
        Include(new PagedRequestValidator());
        RuleFor(request => request.Status).IsInEnum();
    }
}
