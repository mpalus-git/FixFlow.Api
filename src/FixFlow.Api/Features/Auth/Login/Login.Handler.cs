using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FixFlow.Api.Features.Auth.Login;

public sealed class LoginHandler(
    UserManager<ApplicationUser> userManager,
    AuthSessionIssuer authSessionIssuer,
    PasswordVerificationTimingGuard passwordVerificationTimingGuard)
{
    public async Task<ErrorOr<AuthTokensResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user))
        {
            passwordVerificationTimingGuard.SimulatePasswordVerification(request.Password);
            return AuthErrors.InvalidCredentials;
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return AuthErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return await authSessionIssuer.StartSessionAsync(user, cancellationToken);
    }
}
