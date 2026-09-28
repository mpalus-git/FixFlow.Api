using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Parts;
using FixFlow.Api.Features.Parts.RestockPart;
using FixFlow.Api.Features.Parts.UpdatePart;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Parts;

public sealed class UpdateArchiveAndRestockPartTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly UpdatePartRequest UpdatedDetails = new("Filtr węglowy", " flt-200 ", 59.50m);

    [Fact]
    public async Task Should_Replace_Part_Details_And_Keep_Stock_When_Dispatcher_Updates_Active_Part()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var part = await client.CreatePartAsync(PartRequests.NewPart("FLT-100"));

        using var response = await client.PutAsJsonAsync(PartRequests.PartUri(part.Id), UpdatedDetails, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var storedPart = await GetPartAsync(client, part.Id);
        storedPart.Name.ShouldBe(UpdatedDetails.Name);
        storedPart.CatalogNumber.ShouldBe("FLT-200");
        storedPart.UnitPrice.ShouldBe(UpdatedDetails.UnitPrice);
        storedPart.StockQuantity.ShouldBe(part.StockQuantity);
        storedPart.CreatedAt.ShouldBe(part.CreatedAt);
    }

    [Fact]
    public async Task Should_Accept_Update_When_Part_Keeps_Its_Own_Catalog_Number_In_Different_Case()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var part = await client.CreatePartAsync(PartRequests.NewPart("FLT-100"));

        using var response = await client.PutAsJsonAsync(PartRequests.PartUri(part.Id), UpdatedDetails with { CatalogNumber = "flt-100" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Update_Uses_Catalog_Number_Of_Another_Part()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreatePartAsync(PartRequests.NewPart("FLT-200"));
        var part = await client.CreatePartAsync(PartRequests.NewPart("FLT-100"));

        using var response = await client.PutAsJsonAsync(PartRequests.PartUri(part.Id), UpdatedDetails, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, PartErrors.DuplicateCatalogNumber.Code);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Archived_Part_Is_Updated()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var part = await client.CreatePartAsync(PartRequests.NewPart());
        await client.ArchivePartAsync(part.Id);

        using var response = await client.PutAsJsonAsync(PartRequests.PartUri(part.Id), UpdatedDetails, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, PartErrors.Archived.Code);
    }

    [Fact]
    public async Task Should_Keep_Archived_Part_Available_By_Identifier_When_Archived_Twice()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var part = await client.CreatePartAsync(PartRequests.NewPart());
        await client.ArchivePartAsync(part.Id);
        var archivedAt = (await GetPartAsync(client, part.Id)).ArchivedAt;

        using var response = await client.PostAsync(PartRequests.ArchivePartUri(part.Id), null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        archivedAt.ShouldNotBeNull();
        (await GetPartAsync(client, part.Id)).ArchivedAt.ShouldBe(archivedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Increase_Stock_When_Delivery_Is_Registered(bool partIsArchived)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var part = await client.CreatePartAsync(PartRequests.NewPart());
        if (partIsArchived)
        {
            await client.ArchivePartAsync(part.Id);
        }

        using var response = await client.PostAsJsonAsync(PartRequests.RestockPartUri(part.Id), new RestockPartRequest(8), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var restockedPart = await response.Content.ReadFromJsonAsync<PartResponse>(TestContext.Current.CancellationToken);
        restockedPart.ShouldNotBeNull().StockQuantity.ShouldBe(part.StockQuantity + 8);
        (await GetPartAsync(client, part.Id)).StockQuantity.ShouldBe(part.StockQuantity + 8);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Delivery_Quantity_Is_Zero()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var part = await client.CreatePartAsync(PartRequests.NewPart());

        using var response = await client.PostAsJsonAsync(PartRequests.RestockPartUri(part.Id), new RestockPartRequest(0), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("quantity");
    }

    [Fact]
    public async Task Should_Return_Concurrent_Modification_When_Part_Stock_Was_Changed_After_It_Was_Loaded()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var part = await client.CreatePartAsync(PartRequests.NewPart());
        await using var firstScope = Factory.Services.CreateAsyncScope();
        await using var secondScope = Factory.Services.CreateAsyncScope();
        var firstDbContext = firstScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var secondDbContext = secondScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var partSeenByFirstRequest = await firstDbContext.Parts.SingleAsync(item => item.Id == part.Id, TestContext.Current.CancellationToken);
        var partSeenBySecondRequest = await secondDbContext.Parts.SingleAsync(item => item.Id == part.Id, TestContext.Current.CancellationToken);

        partSeenByFirstRequest.Restock(5);
        var firstDelivery = await firstDbContext.SaveChangesOrConflictAsync(TestContext.Current.CancellationToken);
        partSeenBySecondRequest.Restock(3);
        var secondDelivery = await secondDbContext.SaveChangesOrConflictAsync(TestContext.Current.CancellationToken);

        firstDelivery.IsError.ShouldBeFalse();
        secondDelivery.FirstError.ShouldBe(SaveChangesConflicts.ConcurrentModification);
        (await GetPartAsync(client, part.Id)).StockQuantity.ShouldBe(part.StockQuantity + 5);
    }

    private static async Task<PartResponse> GetPartAsync(HttpClient client, Guid partId)
    {
        var part = await client.GetFromJsonAsync<PartResponse>(PartRequests.PartUri(partId), TestContext.Current.CancellationToken);
        return part.ShouldNotBeNull();
    }
}
