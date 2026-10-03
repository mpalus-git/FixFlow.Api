using FixFlow.Api.Domain.Users;
using FluentValidation;

namespace FixFlow.Api.Features.Users.UpdateUser;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(ApplicationUser.FullNameMaxLength);
    }
}
