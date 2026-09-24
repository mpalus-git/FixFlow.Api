using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Domain.Users;
using FluentValidation;

namespace FixFlow.Api.Features.Users.ListUsers;

public sealed class ListUsersRequestValidator : AbstractValidator<ListUsersRequest>
{
    public ListUsersRequestValidator()
    {
        Include(new PagedRequestValidator());
        RuleFor(request => request.Role)
            .Must(role => Roles.All.Contains(role))
            .When(request => request.Role is not null)
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");
    }
}
