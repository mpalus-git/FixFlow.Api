using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Parts;
using FixFlow.Api.Features.ServiceEntries.AddServiceEntry;
using FixFlow.Api.IntegrationTests.Parts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FixFlow.Api.IntegrationTests.ServiceEntries;

public sealed class StockRuleTests(FixFlowApiFactory factory) : ServiceEntryTestBase(factory)
{
    [Fact]
    public async Task Should_Decrease_Stock_And_Record_Current_Price_When_Parts_Are_Used_In_Work_Entry()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var part = await scenario.DispatcherClient.CreatePartAsync(PartRequests.NewPart() with { StockQuantity = 5, UnitPrice = 40m });

        var entry = await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.WorkEntry(scenario.WorkOrder, new ServiceEntryPartRequest(part.Id, 5)));

        var usedPart = entry.Parts.ShouldHaveSingleItem();
        usedPart.Quantity.ShouldBe(5);
        usedPart.UnitPrice.ShouldBe(40m);
        (await GetPartAsync(scenario.DispatcherClient, part.Id)).StockQuantity.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Reject_Work_Entry_And_Keep_Stock_When_Part_Usage_Would_Drop_Stock_Below_Zero()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var sufficientPart = await scenario.DispatcherClient.CreatePartAsync(PartRequests.NewPart("FLT-100") with { StockQuantity = 5 });
        var insufficientPart = await scenario.DispatcherClient.CreatePartAsync(PartRequests.NewPart("FLT-200") with { StockQuantity = 2 });
        var request = ServiceEntryRequests.WorkEntry(
            scenario.WorkOrder,
            new ServiceEntryPartRequest(sufficientPart.Id, 1),
            new ServiceEntryPartRequest(insufficientPart.Id, 3));

        using var response = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, PartErrors.InsufficientStock.Code);
        (await GetPartAsync(scenario.DispatcherClient, sufficientPart.Id)).StockQuantity.ShouldBe(5);
        (await GetPartAsync(scenario.DispatcherClient, insufficientPart.Id)).StockQuantity.ShouldBe(2);
        (await scenario.DispatcherClient.ListServiceEntriesAsync(scenario.WorkOrder.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Reject_Work_Entry_When_Used_Part_Is_Archived()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var part = await scenario.DispatcherClient.CreatePartAsync(PartRequests.NewPart());
        await scenario.DispatcherClient.ArchivePartAsync(part.Id);

        using var response = await scenario.TechnicianClient.PostServiceEntryAsync(
            scenario.WorkOrder.Id,
            ServiceEntryRequests.WorkEntry(scenario.WorkOrder, new ServiceEntryPartRequest(part.Id, 1)));

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, PartErrors.Archived.Code);
    }

    [Fact]
    public async Task Should_Reject_Negative_Stock_In_Database_When_Domain_Check_Is_Bypassed()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var part = await client.CreatePartAsync(PartRequests.NewPart());
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();

        var exception = await Should.ThrowAsync<PostgresException>(() =>
            dbContext.Database.ExecuteSqlAsync($"UPDATE parts SET stock_quantity = -1 WHERE id = {part.Id}", TestContext.Current.CancellationToken));

        exception.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        exception.ConstraintName.ShouldBe(PartConfiguration.StockQuantityCheckName);
    }

    private static async Task<PartResponse> GetPartAsync(HttpClient client, Guid partId)
    {
        var part = await client.GetFromJsonAsync<PartResponse>(PartRequests.PartUri(partId), TestContext.Current.CancellationToken);
        return part.ShouldNotBeNull();
    }
}
