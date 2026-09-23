using FixFlow.Api.Common.Pagination;
using FluentValidation;

namespace FixFlow.Api.Features.Clients.ListClients;

public sealed class ListClientsRequestValidator : AbstractValidator<ListClientsRequest>
{
    public ListClientsRequestValidator()
    {
        Include(new PagedRequestValidator());
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
