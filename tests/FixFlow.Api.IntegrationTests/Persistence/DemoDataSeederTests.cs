using System.Net;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Seeding;
using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.IntegrationTests.Persistence;

public sealed class DemoDataSeederTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Create_Demo_Inventory_Once_When_Database_Is_Empty_And_Seeding_Runs_Twice()
    {
        await SeedDemoDataAsync();
        await SeedDemoDataAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var cancellationToken = TestContext.Current.CancellationToken;
        (await dbContext.Clients.CountAsync(cancellationToken)).ShouldBe(6);
        (await dbContext.Clients.CountAsync(client => client.ArchivedAt != null, cancellationToken)).ShouldBe(1);
        (await dbContext.Devices.CountAsync(cancellationToken)).ShouldBe(13);
        (await dbContext.Devices.CountAsync(device => device.ArchivedAt != null, cancellationToken)).ShouldBe(1);
        (await dbContext.Parts.CountAsync(cancellationToken)).ShouldBe(11);
        (await dbContext.Parts.CountAsync(part => part.ArchivedAt != null, cancellationToken)).ShouldBe(1);
        (await dbContext.WorkOrders.CountAsync(cancellationToken)).ShouldBe(23);
    }

    [Fact]
    public async Task Should_Create_WorkOrders_In_Every_Status_When_Demo_Data_Is_Seeded()
    {
        await SeedDemoDataAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var workOrders = await dbContext.WorkOrders.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        var countsByStatus = workOrders.CountBy(workOrder => workOrder.Status).ToDictionary();
        countsByStatus.ShouldBe(new Dictionary<WorkOrderStatus, int>
        {
            [WorkOrderStatus.New] = 4,
            [WorkOrderStatus.Assigned] = 3,
            [WorkOrderStatus.InProgress] = 3,
            [WorkOrderStatus.Completed] = 3,
            [WorkOrderStatus.Invoiced] = 10,
        },
        ignoreOrder: true);
        workOrders.Count(workOrder => workOrder.IsOverdue).ShouldBe(3);
        workOrders.Where(workOrder => workOrder.Status == WorkOrderStatus.InProgress)
            .Select(workOrder => workOrder.TechnicianId)
            .ShouldBeUnique();
    }

    [Fact]
    public async Task Should_Keep_Stock_Consistent_With_Part_Usage_When_Demo_Data_Is_Seeded()
    {
        await SeedDemoDataAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var stockByCatalogNumber = await dbContext.Parts.ToDictionaryAsync(
            part => part.CatalogNumber,
            part => part.StockQuantity,
            TestContext.Current.CancellationToken);
        stockByCatalogNumber["RLR-FEED-01"].ShouldBe(26);
        stockByCatalogNumber["FSR-UNI-220"].ShouldBe(2);
        stockByCatalogNumber["REF-R32-1KG"].ShouldBe(23);
        stockByCatalogNumber["FLT-AC-100"].ShouldBe(34);
        stockByCatalogNumber["SNS-NTC-10K"].ShouldBe(18);
        stockByCatalogNumber["BLT-TRF-01"].ShouldBe(1);
        stockByCatalogNumber["PMP-CND-01"].ShouldBe(0);
        stockByCatalogNumber["TNR-RIC-C3000-K"].ShouldBe(7);
        stockByCatalogNumber["FLT-CARB-OLD"].ShouldBe(3);
        (await dbContext.ServiceEntries.CountAsync(entry => entry.IsCorrection, TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Create_Additional_Technicians_Without_Password_When_Demo_Data_Is_Seeded()
    {
        await SeedDemoDataAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var email in DemoDataSeeder.AdditionalTechnicianEmails)
        {
            var technician = (await userManager.FindByEmailAsync(email)).ShouldNotBeNull();
            (await userManager.IsInRoleAsync(technician, Roles.Technician)).ShouldBeTrue();
            (await userManager.HasPasswordAsync(technician)).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Should_Seed_Deactivated_Technician_With_Invoiced_History_When_Demo_Data_Is_Seeded()
    {
        await SeedDemoDataAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var formerTechnician = (await userManager.FindByEmailAsync(DemoDataSeeder.PiotrZielinskiEmail)).ShouldNotBeNull();
        formerTechnician.IsActive.ShouldBeFalse();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var statuses = await dbContext.WorkOrders
            .Where(workOrder => workOrder.TechnicianId == formerTechnician.Id)
            .Select(workOrder => workOrder.Status)
            .ToListAsync(TestContext.Current.CancellationToken);
        statuses.Count.ShouldBe(5);
        statuses.ShouldAllBe(status => status == WorkOrderStatus.Invoiced);
    }

    [Fact]
    public async Task Should_Generate_Service_Protocol_For_Seeded_WorkOrder_With_Correction()
    {
        await SeedDemoDataAsync();
        Guid workOrderId;
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
            workOrderId = await dbContext.ServiceEntries
                .Where(entry => entry.IsCorrection)
                .Select(entry => entry.WorkOrderId)
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        using var response = await client.GetAsync(
            new Uri($"/api/v1/work-orders/{workOrderId}/protocol", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Not_Seed_Demo_Data_When_Clients_Already_Exist()
    {
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
            var existingClient = Client.Create("Existing client", new Address("Street", "1", "00-001", "City"), "Contact", "123456789", null, DateTimeOffset.UtcNow);
            existingClient.Archive(DateTimeOffset.UtcNow);
            dbContext.Clients.Add(existingClient);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await SeedDemoDataAsync();

        await using var verificationScope = Factory.Services.CreateAsyncScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        (await verificationDbContext.Clients.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        (await verificationDbContext.Parts.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        (await verificationDbContext.WorkOrders.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Seed_Demo_Data_On_Startup_When_Demo_Data_Is_Enabled()
    {
        await using var factoryWithDemoData = Factory.WithWebHostBuilder(builder => builder.UseSetting("Seed:DemoData:Enabled", "true"));

        await using var scope = factoryWithDemoData.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        (await dbContext.WorkOrders.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(23);
    }

    [Fact]
    public async Task Should_Fail_Startup_When_Demo_Data_Is_Enabled_Without_Demo_Users()
    {
        await using var misconfiguredFactory = Factory.WithWebHostBuilder(builder => builder
            .UseSetting("Seed:DemoData:Enabled", "true")
            .UseSetting("Seed:DemoUsers:Enabled", "false"));

        var exception = Should.Throw<OptionsValidationException>(() => misconfiguredFactory.Services);

        exception.OptionsType.ShouldBe(typeof(DemoDataOptions));
    }

    private async Task SeedDemoDataAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedIfDatabaseIsEmptyAsync(TestContext.Current.CancellationToken);
    }
}
