using FluentValidation;

namespace FixFlow.Api.Features.Users.ResetPassword;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
