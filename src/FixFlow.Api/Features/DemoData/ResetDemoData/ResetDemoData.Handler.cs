using ErrorOr;
using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Seeding;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Features.DemoData.ResetDemoData;

public sealed partial class ResetDemoDataHandler(
    FixFlowDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IdentitySeeder identitySeeder,
    DemoDataSeeder demoDataSeeder,
    HybridCache cache,
    IOptions<DemoDataOptions> demoDataOptions,
    IOptions<DemoUsersOptions> demoUsersOptions,
    TimeProvider timeProvider,
    ILogger<ResetDemoDataHandler> logger)
{
    public async Task<ErrorOr<Success>> HandleAsync(CancellationToken cancellationToken)
    {
        if (!demoDataOptions.Value.Enabled)
        {
            return DemoDataErrors.Disabled;
        }

        await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(ResetInTransactionAsync, cancellationToken);
        await cache.RemoveByTagAsync([CacheTags.Clients, CacheTags.Devices], cancellationToken);
        LogDemoDataReset();

        return Result.Success;
    }

    private async Task ResetInTransactionAsync(CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.ServiceEntries.ExecuteDeleteAsync(cancellationToken);
        await dbContext.WorkOrders.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Photos.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Devices.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Clients.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Parts.ExecuteDeleteAsync(cancellationToken);
        await dbContext.WorkOrderNumberCounters.ExecuteDeleteAsync(cancellationToken);

        await identitySeeder.SeedAsync();
        var demoUsers = demoUsersOptions.Value;
        await RestoreDemoAccountAsync(DemoUsersOptions.DispatcherEmail, demoUsers.DispatcherPassword, cancellationToken);
        await RestoreDemoAccountAsync(DemoUsersOptions.TechnicianEmail, demoUsers.TechnicianPassword, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await demoDataSeeder.SeedWithinCurrentTransactionAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RestoreDemoAccountAsync(string email, string password, CancellationToken cancellationToken)
    {
        var account = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"Demo account '{email}' does not exist.");

        account.Activate();
        (await userManager.RemovePasswordAsync(account)).ThrowIfFailed(email);
        (await userManager.AddPasswordAsync(account, password)).ThrowIfFailed(email);
        (await userManager.SetLockoutEndDateAsync(account, null)).ThrowIfFailed(email);
        (await userManager.ResetAccessFailedCountAsync(account)).ThrowIfFailed(email);
        await dbContext.RevokeActiveRefreshTokensOfUserAsync(account.Id, timeProvider.GetUtcNow(), cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo data reset to its initial state")]
    private partial void LogDemoDataReset();
}
