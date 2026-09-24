using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Parts;

namespace FixFlow.Api.IntegrationTests.Parts;

public sealed class ListPartsTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Requested_Page_Ordered_By_Name_And_Catalog_Number_When_Parts_Exceed_Page_Size()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreatePartAsync(PartRequests.NewPart("P-3", "Wentylator"));
        await client.CreatePartAsync(PartRequests.NewPart("P-2", "Filtr"));
        await client.CreatePartAsync(PartRequests.NewPart("P-5", "Czujnik"));
        await client.CreatePartAsync(PartRequests.NewPart("P-1", "Filtr"));
        await client.CreatePartAsync(PartRequests.NewPart("P-4", "Sprężarka"));

        var page = await GetPageAsync(client, "?page=2&pageSize=2");

        page.Items.Select(item => item.CatalogNumber).ShouldBe(["P-2", "P-4"]);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(2);
        page.TotalCount.ShouldBe(5);
    }

    [Theory]
    [InlineData("flt-2")]
    [InlineData("WĘGL")]
    public async Task Should_Return_Parts_Matching_Name_Or_Catalog_Number_When_Search_Is_Provided(string search)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreatePartAsync(PartRequests.NewPart("FLT-1", "Filtr powietrza"));
        await client.CreatePartAsync(PartRequests.NewPart("FLT-2", "Filtr węglowy"));

        var page = await GetPageAsync(client, $"?search={Uri.EscapeDataString(search)}");

        page.Items.Select(item => item.CatalogNumber).ShouldBe(["FLT-2"]);
    }

    [Fact]
    public async Task Should_Not_List_Archived_Parts_When_Listing()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreatePartAsync(PartRequests.NewPart("P-ACTIVE"));
        var archivedPart = await client.CreatePartAsync(PartRequests.NewPart("P-ARCHIVED"));
        await client.ArchivePartAsync(archivedPart.Id);

        var page = await GetPageAsync(client, string.Empty);

        page.Items.Select(item => item.CatalogNumber).ShouldBe(["P-ACTIVE"]);
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Allow_Technician_To_List_Parts_With_Stock_Quantities()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await dispatcherClient.CreatePartAsync(PartRequests.NewPart());
        using var technicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        var page = await GetPageAsync(technicianClient, string.Empty);

        page.Items.ShouldHaveSingleItem().StockQuantity.ShouldBe(PartRequests.NewPart().StockQuantity);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Search_Is_Longer_Than_One_Hundred_Characters()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await client.GetAsync(new Uri($"/api/v1/parts?search={new string('a', 101)}", UriKind.Relative), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("Search");
    }

    private static async Task<PagedResponse<PartResponse>> GetPageAsync(HttpClient client, string query)
    {
        using var response = await client.GetAsync(new Uri($"/api/v1/parts{query}", UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<PartResponse>>(TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }
}
