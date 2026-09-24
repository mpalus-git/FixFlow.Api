using FixFlow.Api.Common.Pagination;
using FluentValidation;

namespace FixFlow.Api.Features.Parts.ListParts;

public sealed class ListPartsRequestValidator : AbstractValidator<ListPartsRequest>
{
    public ListPartsRequestValidator()
    {
        Include(new PagedRequestValidator());
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
