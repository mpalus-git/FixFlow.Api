using FixFlow.Api.Common.Pagination;
using FluentValidation;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrders;

public sealed class ListWorkOrdersRequestValidator : AbstractValidator<ListWorkOrdersRequest>
{
    public ListWorkOrdersRequestValidator()
    {
        Include(new PagedRequestValidator());
        RuleForEach(request => request.Status).IsInEnum();
        RuleFor(request => request.DueTo)
            .GreaterThanOrEqualTo(request => request.DueFrom)
            .When(request => request.DueFrom is not null && request.DueTo is not null)
            .WithMessage("The end of the due date range cannot be before its start.");
        RuleFor(request => request.Search).MaximumLength(100);
        RuleFor(request => request.SortBy)
            .Must(sortBy => Enum.GetNames<WorkOrderSortField>().Contains(sortBy))
            .WithMessage($"Sort field must be one of: {string.Join(", ", Enum.GetNames<WorkOrderSortField>())}.");
        RuleFor(request => request.SortDirection)
            .Must(sortDirection => Enum.GetNames<SortDirection>().Contains(sortDirection))
            .WithMessage($"Sort direction must be one of: {string.Join(", ", Enum.GetNames<SortDirection>())}.");
    }
}
