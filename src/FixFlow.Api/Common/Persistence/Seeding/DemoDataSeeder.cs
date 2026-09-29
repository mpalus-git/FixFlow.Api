using FixFlow.Api.Common.Caching;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.WorkOrders.MarkOverdueWorkOrders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Common.Persistence.Seeding;

public sealed partial class DemoDataSeeder(
    FixFlowDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    MarkOverdueWorkOrdersHandler markOverdueWorkOrdersHandler,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<DemoDataSeeder> logger)
{
    public const string AnnaKowalczykEmail = "anna.kowalczyk@fixflow.local";
    public const string TomaszWojcikEmail = "tomasz.wojcik@fixflow.local";

    public static readonly IReadOnlyList<string> AdditionalTechnicianEmails = [AnnaKowalczykEmail, TomaszWojcikEmail];

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

        var technicians = new DemoTechnicians(
            await FindLoginTechnicianIdAsync(),
            await EnsureTechnicianWithoutPasswordAsync(AnnaKowalczykEmail),
            await EnsureTechnicianWithoutPasswordAsync(TomaszWojcikEmail));

        var now = timeProvider.GetUtcNow();
        var inventory = DemoInventory.Create(now - InventoryAge);
        var history = DemoWorkOrderHistory.Create(inventory, technicians, now);
        inventory.ArchiveRetiredItems(now - RetiredItemsAge);
        dbContext.Clients.AddRange(inventory.Clients);
        dbContext.Devices.AddRange(inventory.Devices);
        dbContext.Parts.AddRange(inventory.Parts);
        dbContext.WorkOrders.AddRange(history.WorkOrders);
        dbContext.ServiceEntries.AddRange(history.ServiceEntries);

        await dbContext.SaveChangesAsync(cancellationToken);
        await markOverdueWorkOrdersHandler.HandleAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        LogDemoDataSeeded(inventory.Clients.Count, inventory.Devices.Count, inventory.Parts.Count, history.WorkOrders.Count);
        return true;
    }

    private async Task<Guid> FindLoginTechnicianIdAsync()
    {
        var loginTechnician = await userManager.FindByEmailAsync(DemoUsersOptions.TechnicianEmail)
            ?? throw new InvalidOperationException("Demo users must be seeded before demo data.");
        return loginTechnician.Id;
    }

    private async Task<Guid> EnsureTechnicianWithoutPasswordAsync(string email)
    {
        if (await userManager.FindByEmailAsync(email) is { } existingTechnician)
        {
            return existingTechnician.Id;
        }

        var technician = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        EnsureSucceeded(await userManager.CreateAsync(technician), email);
        EnsureSucceeded(await userManager.AddToRoleAsync(technician, Roles.Technician), email);
        return technician.Id;
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo data: {ClientCount} clients, {DeviceCount} devices, {PartCount} parts, {WorkOrderCount} work orders")]
    private partial void LogDemoDataSeeded(int clientCount, int deviceCount, int partCount, int workOrderCount);
}
