using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Users.ChangePassword;

public sealed class ChangePasswordHandler(
    UserManager<ApplicationUser> userManager,
    FixFlowDbContext dbContext,
    AuthSessionIssuer authSessionIssuer,
    TimeProvider timeProvider)
{
    private const string PasswordMismatchCode = "PasswordMismatch";

    public Task<ErrorOr<AuthTokensResponse>> HandleAsync(ClaimsPrincipal principal, ChangePasswordRequest request, CancellationToken cancellationToken) =>
        dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            new PasswordChange(principal.GetUserId(), request),
            ChangePasswordInTransactionAsync,
            cancellationToken);

    private async Task<ErrorOr<AuthTokensResponse>> ChangePasswordInTransactionAsync(PasswordChange passwordChange, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var user = await userManager.FindByIdAsync(passwordChange.UserId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var change = await userManager.ChangePasswordAsync(user, passwordChange.Request.CurrentPassword, passwordChange.Request.NewPassword);
        if (!change.Succeeded)
        {
            return ToErrors(change);
        }

        await dbContext.RevokeActiveRefreshTokensOfUserAsync(user.Id, timeProvider.GetUtcNow(), cancellationToken);
        var tokens = await authSessionIssuer.StartSessionAsync(user, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return tokens;
    }

    private static List<Error> ToErrors(IdentityResult result) =>
        result.Errors
            .Select(error => error.Code switch
            {
                PasswordMismatchCode => Error.Validation(nameof(ChangePasswordRequest.CurrentPassword), "The current password is incorrect."),
                _ when error.Code.StartsWith("Password", StringComparison.Ordinal) => Error.Validation(nameof(ChangePasswordRequest.NewPassword), error.Description),
                _ => Error.Failure($"User.{error.Code}", error.Description),
            })
            .ToList();

    private sealed record PasswordChange(Guid UserId, ChangePasswordRequest Request);
}
