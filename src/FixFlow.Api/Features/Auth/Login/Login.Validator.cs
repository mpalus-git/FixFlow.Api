using FluentValidation;

namespace FixFlow.Api.Features.Auth.Login;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(128);
    }
}
