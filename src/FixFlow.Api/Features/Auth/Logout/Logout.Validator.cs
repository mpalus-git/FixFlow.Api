using FluentValidation;

namespace FixFlow.Api.Features.Auth.Logout;

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty().MaximumLength(256);
    }
}
