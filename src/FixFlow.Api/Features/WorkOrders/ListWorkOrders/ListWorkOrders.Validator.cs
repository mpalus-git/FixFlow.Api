using FixFlow.Api.Common.Pagination;
using FluentValidation;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrders;

public sealed class ListWorkOrdersRequestValidator : AbstractValidator<ListWorkOrdersRequest>
{
    public ListWorkOrdersRequestValidator()
    {
        Include(new PagedRequestValidator());
        RuleFor(request => request.Status).IsInEnum();
        RuleFor(request => request.DueTo)
            .GreaterThanOrEqualTo(request => request.DueFrom)
            .When(request => request.DueFrom is not null && request.DueTo is not null)
            .WithMessage("The end of the due date range cannot be before its start.");
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
