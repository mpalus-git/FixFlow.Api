using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Users.ResetPassword;

public sealed class ResetPasswordHandler(UserManager<ApplicationUser> userManager, FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public Task<ErrorOr<Success>> HandleAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken) =>
        dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            new PasswordReset(userId, request.NewPassword),
            ResetPasswordInTransactionAsync,
            cancellationToken);

    private async Task<ErrorOr<Success>> ResetPasswordInTransactionAsync(PasswordReset passwordReset, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var user = await userManager.FindByIdAsync(passwordReset.UserId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var removal = await userManager.RemovePasswordAsync(user);
        if (!removal.Succeeded)
        {
            return ToErrors(removal);
        }

        var addition = await userManager.AddPasswordAsync(user, passwordReset.NewPassword);
        if (!addition.Succeeded)
        {
            return ToErrors(addition);
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        await dbContext.RevokeActiveRefreshTokensOfUserAsync(user.Id, timeProvider.GetUtcNow(), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success;
    }

    private static List<Error> ToErrors(IdentityResult result) =>
        result.Errors
            .Select(error => error.Code.StartsWith("Password", StringComparison.Ordinal)
                ? Error.Validation(nameof(ResetPasswordRequest.NewPassword), error.Description)
                : Error.Failure($"User.{error.Code}", error.Description))
            .ToList();

    private sealed record PasswordReset(Guid UserId, string NewPassword);
}
