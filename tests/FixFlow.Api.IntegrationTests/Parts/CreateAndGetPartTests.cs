using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Parts;

namespace FixFlow.Api.IntegrationTests.Parts;

public sealed class CreateAndGetPartTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData(Roles.Dispatcher)]
    [InlineData(Roles.Admin)]
    public async Task Should_Create_Part_With_Location_When_Dispatcher_Or_Admin_Creates_It(string role)
    {
        using var client = await CreateAuthenticatedClientAsync(role);
        var request = PartRequests.NewPart();

        using var response = await client.PostAsJsonAsync(PartRequests.PartsUri, request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdPart = await response.Content.ReadFromJsonAsync<PartResponse>(TestContext.Current.CancellationToken);
        createdPart.ShouldNotBeNull();
        response.Headers.Location.ShouldBe(PartRequests.PartUri(createdPart.Id));
        createdPart.Name.ShouldBe(request.Name);
        createdPart.StockQuantity.ShouldBe(request.StockQuantity);
        createdPart.UnitPrice.ShouldBe(request.UnitPrice);
        createdPart.ArchivedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Store_Trimmed_Upper_Case_Catalog_Number_When_Part_Is_Created()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        var createdPart = await client.CreatePartAsync(PartRequests.NewPart("  flt-100x "));

        createdPart.CatalogNumber.ShouldBe("FLT-100X");
    }

    [Theory]
    [InlineData("FLT-100")]
    [InlineData(" flt-100 ")]
    public async Task Should_Return_Conflict_Problem_When_Catalog_Number_Is_Already_Used(string duplicateCatalogNumber)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreatePartAsync(PartRequests.NewPart("FLT-100"));

        using var response = await client.PostAsJsonAsync(PartRequests.PartsUri, PartRequests.NewPart(duplicateCatalogNumber), TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, PartErrors.DuplicateCatalogNumber.Code);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Catalog_Number_Belongs_To_Archived_Part()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var archivedPart = await client.CreatePartAsync(PartRequests.NewPart("FLT-100"));
        await client.ArchivePartAsync(archivedPart.Id);

        using var response = await client.PostAsJsonAsync(PartRequests.PartsUri, PartRequests.NewPart("FLT-100"), TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, PartErrors.DuplicateCatalogNumber.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Unit_Price_Has_More_Than_Two_Decimal_Places()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var request = PartRequests.NewPart() with { UnitPrice = 49.999m };

        using var response = await client.PostAsJsonAsync(PartRequests.PartsUri, request, TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("unitPrice");
    }

    [Fact]
    public async Task Should_Return_Bad_Request_When_Unit_Price_Is_Sent_As_Text()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var request = new { name = "Filtr powietrza", catalogNumber = "FLT-100", stockQuantity = 5, unitPrice = "12.50" };

        using var response = await client.PostAsJsonAsync(PartRequests.PartsUri, request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_Technician_Creates_Part()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await client.PostAsJsonAsync(PartRequests.PartsUri, PartRequests.NewPart(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Return_Part_When_Technician_Gets_Existing_Part()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdPart = await dispatcherClient.CreatePartAsync(PartRequests.NewPart());
        using var technicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        var fetchedPart = await technicianClient.GetFromJsonAsync<PartResponse>(PartRequests.PartUri(createdPart.Id), TestContext.Current.CancellationToken);

        fetchedPart.ShouldBe(createdPart);
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Part_Does_Not_Exist()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await client.GetAsync(PartRequests.PartUri(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, PartErrors.NotFound.Code);
    }
}
