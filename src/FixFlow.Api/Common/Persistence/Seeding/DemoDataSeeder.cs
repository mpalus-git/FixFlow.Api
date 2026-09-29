using FixFlow.Api.Common.Caching;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Common.Persistence.Seeding;

public sealed partial class DemoDataSeeder(
    FixFlowDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<DemoDataSeeder> logger)
{
    public static readonly IReadOnlyList<string> AdditionalTechnicianEmails =
    [
        "anna.kowalczyk@fixflow.local",
        "tomasz.wojcik@fixflow.local",
    ];

    private static readonly TimeSpan InventoryAge = TimeSpan.FromDays(90);
    private static readonly TimeSpan RetiredItemsAge = TimeSpan.FromDays(7);

    public async Task SeedIfDatabaseIsEmptyAsync(CancellationToken cancellationToken)
    {
        var seeded = await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(SeedInTransactionAsync, cancellationToken);
        if (seeded)
        {
            await cache.RemoveByTagAsync([CacheTags.Clients, CacheTags.Devices], cancellationToken);
        }
    }

    private async Task<bool> SeedInTransactionAsync(CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (await dbContext.Clients.AnyAsync(cancellationToken))
        {
            LogDemoDataSkipped();
            return false;
        }

        foreach (var email in AdditionalTechnicianEmails)
        {
            await EnsureTechnicianWithoutPasswordAsync(email);
        }

        var now = timeProvider.GetUtcNow();
        var inventory = DemoInventory.Create(now - InventoryAge);
        dbContext.Clients.AddRange(inventory.Clients);
        dbContext.Devices.AddRange(inventory.Devices);
        dbContext.Parts.AddRange(inventory.Parts);
        inventory.ArchiveRetiredItems(now - RetiredItemsAge);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        LogDemoDataSeeded(inventory.Clients.Count, inventory.Devices.Count, inventory.Parts.Count);
        return true;
    }

    private async Task EnsureTechnicianWithoutPasswordAsync(string email)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var technician = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        EnsureSucceeded(await userManager.CreateAsync(technician), email);
        EnsureSucceeded(await userManager.AddToRoleAsync(technician, Roles.Technician), email);
    }

    private static void EnsureSucceeded(IdentityResult result, string subject)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Seeding '{subject}' failed: {errors}");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo data skipped because the database already contains clients")]
    private partial void LogDemoDataSkipped();

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo data: {ClientCount} clients, {DeviceCount} devices, {PartCount} parts")]
    private partial void LogDemoDataSeeded(int clientCount, int deviceCount, int partCount);
}
