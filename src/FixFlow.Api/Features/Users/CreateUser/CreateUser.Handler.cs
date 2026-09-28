using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Users.CreateUser;

public sealed class CreateUserHandler(UserManager<ApplicationUser> userManager, FixFlowDbContext dbContext)
{
    private static readonly string[] DuplicateErrorCodes = ["DuplicateEmail", "DuplicateUserName"];

    public Task<ErrorOr<UserResponse>> HandleAsync(CreateUserRequest request, CancellationToken cancellationToken) =>
        dbContext.Database.CreateExecutionStrategy().ExecuteAsync(request, CreateUserInTransactionAsync, cancellationToken);

    private async Task<ErrorOr<UserResponse>> CreateUserInTransactionAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var creation = await userManager.CreateAsync(user, request.Password);
        if (!creation.Succeeded)
        {
            return ToErrors(creation);
        }

        var roleAssignment = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleAssignment.Succeeded)
        {
            return ToErrors(roleAssignment);
        }

        await transaction.CommitAsync(cancellationToken);

        return new UserResponse(user.Id, request.Email, request.Role, user.IsActive);
    }

    private static List<Error> ToErrors(IdentityResult result)
    {
        if (result.Errors.Any(error => DuplicateErrorCodes.Contains(error.Code)))
        {
            return [UserErrors.DuplicateEmail];
        }

        return result.Errors
            .Select(error => error.Code.StartsWith("Password", StringComparison.Ordinal)
                ? Error.Validation(nameof(CreateUserRequest.Password), error.Description)
                : Error.Failure($"User.{error.Code}", error.Description))
            .ToList();
    }
}
