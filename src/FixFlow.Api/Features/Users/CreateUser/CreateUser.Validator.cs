using FixFlow.Api.Domain.Users;
using FluentValidation;

namespace FixFlow.Api.Features.Users.CreateUser;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(ApplicationUser.FullNameMaxLength);
        RuleFor(request => request.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(request => request.Role)
            .NotEmpty()
            .Must(role => Roles.All.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");
    }
}
