using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Seeding;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.WorkOrders;
using FixFlow.Api.Features.WorkOrders.MarkOverdueWorkOrders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Features.DemoData;

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
    public const string PiotrZielinskiEmail = "piotr.zielinski@fixflow.local";

    public static readonly IReadOnlyList<string> AdditionalTechnicianEmails = [AnnaKowalczykEmail, TomaszWojcikEmail, PiotrZielinskiEmail];

    private static readonly TimeSpan InventoryAge = TimeSpan.FromDays(90);
    private static readonly TimeSpan RetiredItemsAge = TimeSpan.FromDays(7);
    private static readonly TimeSpan FormerTechnicianDeactivationAge = TimeSpan.FromDays(56);

    public async Task SeedIfDatabaseIsEmptyAsync(CancellationToken cancellationToken)
    {
        var seeded = await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(SeedInOwnTransactionAsync, cancellationToken);
        if (seeded)
        {
            await cache.RemoveByTagAsync([CacheTags.Clients, CacheTags.Devices], cancellationToken);
        }
    }

    public async Task<bool> SeedWithinCurrentTransactionAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Clients.AnyAsync(cancellationToken))
        {
            LogDemoDataSkipped();
            return false;
        }

        var now = timeProvider.GetUtcNow();
        var technicians = new DemoTechnicians(
            await FindLoginTechnicianIdAsync(),
            (await EnsureTechnicianWithoutPasswordAsync(AnnaKowalczykEmail, "Anna Kowalczyk")).Id,
            (await EnsureTechnicianWithoutPasswordAsync(TomaszWojcikEmail, "Tomasz Wójcik")).Id,
            await EnsureDeactivatedTechnicianAsync(PiotrZielinskiEmail, "Piotr Zieliński", now - FormerTechnicianDeactivationAge));

        var inventory = DemoInventory.Create(now - InventoryAge);
        var history = DemoWorkOrderHistory.Create(inventory, technicians, now);
        inventory.ArchiveRetiredItems(now - RetiredItemsAge);
        foreach (var workOrder in history.WorkOrders.OrderBy(workOrder => workOrder.CreatedAt).ThenBy(workOrder => workOrder.Id))
        {
            await dbContext.AssignNextNumberAsync(workOrder, cancellationToken);
        }

        dbContext.Clients.AddRange(inventory.Clients);
        dbContext.Devices.AddRange(inventory.Devices);
        dbContext.Parts.AddRange(inventory.Parts);
        dbContext.WorkOrders.AddRange(history.WorkOrders);
        dbContext.ServiceEntries.AddRange(history.ServiceEntries);

        await dbContext.SaveChangesAsync(cancellationToken);
        await markOverdueWorkOrdersHandler.HandleAsync(cancellationToken);

        LogDemoDataSeeded(inventory.Clients.Count, inventory.Devices.Count, inventory.Parts.Count, history.WorkOrders.Count);
        return true;
    }

    private async Task<bool> SeedInOwnTransactionAsync(CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var seeded = await SeedWithinCurrentTransactionAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return seeded;
    }

    private async Task<Guid> FindLoginTechnicianIdAsync()
    {
        var loginTechnician = await userManager.FindByEmailAsync(DemoUsersOptions.TechnicianEmail)
            ?? throw new InvalidOperationException("Demo users must be seeded before demo data.");
        return loginTechnician.Id;
    }

    private async Task<Guid> EnsureDeactivatedTechnicianAsync(string email, string fullName, DateTimeOffset deactivatedAt)
    {
        var technician = await EnsureTechnicianWithoutPasswordAsync(email, fullName);
        if (technician.IsActive)
        {
            technician.Deactivate(deactivatedAt);
            (await userManager.UpdateAsync(technician)).ThrowIfFailed(email);
        }

        return technician.Id;
    }

    private async Task<ApplicationUser> EnsureTechnicianWithoutPasswordAsync(string email, string fullName)
    {
        if (await userManager.FindByEmailAsync(email) is { } existingTechnician)
        {
            await userManager.EnsureFullNameAsync(existingTechnician, fullName);
            return existingTechnician;
        }

        var technician = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };
        technician.ChangeFullName(fullName);

        (await userManager.CreateAsync(technician)).ThrowIfFailed(email);
        (await userManager.AddToRoleAsync(technician, Roles.Technician)).ThrowIfFailed(email);
        return technician;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo data skipped because the database already contains clients")]
    private partial void LogDemoDataSkipped();

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo data: {ClientCount} clients, {DeviceCount} devices, {PartCount} parts, {WorkOrderCount} work orders")]
    private partial void LogDemoDataSeeded(int clientCount, int deviceCount, int partCount, int workOrderCount);
}
