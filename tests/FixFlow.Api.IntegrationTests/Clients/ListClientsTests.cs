using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Clients;

public sealed class ListClientsTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Requested_Page_Ordered_By_Name_With_Total_Count_When_Clients_Exceed_Page_Size()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        foreach (var name in new[] { "Delta", "Alfa", "Echo", "Charlie", "Bravo" })
        {
            await client.CreateClientAsync(ClientRequests.NewClient(name));
        }

        var page = await GetPageAsync(client, "?page=2&pageSize=2");

        page.Items.Select(item => item.Name).ShouldBe(["Charlie", "Delta"]);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(2);
        page.TotalCount.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Not_List_Archived_Clients_When_Listing()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreateClientAsync(ClientRequests.NewClient("Active client"));
        var archivedClient = await client.CreateClientAsync(ClientRequests.NewClient("Archived client"));
        await ArchiveAsync(archivedClient.Id);

        var page = await GetPageAsync(client, string.Empty);

        page.Items.Select(item => item.Name).ShouldBe(["Active client"]);
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Return_Clients_Matching_Name_Fragment_Regardless_Of_Case_When_Search_Is_Provided()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreateClientAsync(ClientRequests.NewClient("Klimat-Serwis"));
        await client.CreateClientAsync(ClientRequests.NewClient("Biuro Rachunkowe"));

        var page = await GetPageAsync(client, "?search=KLIMAT");

        page.Items.Select(item => item.Name).ShouldBe(["Klimat-Serwis"]);
    }

    [Fact]
    public async Task Should_Treat_Wildcard_Characters_Literally_When_Search_Contains_Them()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreateClientAsync(ClientRequests.NewClient("Klimat-Serwis"));
        await client.CreateClientAsync(ClientRequests.NewClient("Rabat 100% Serwis"));

        var page = await GetPageAsync(client, "?search=%25");

        page.Items.Select(item => item.Name).ShouldBe(["Rabat 100% Serwis"]);
    }

    [Fact]
    public async Task Should_Allow_Technician_To_List_Clients()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await dispatcherClient.CreateClientAsync(ClientRequests.NewClient());
        using var technicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        var page = await GetPageAsync(technicianClient, string.Empty);

        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Page_Size_Exceeds_Maximum()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await client.GetAsync(new Uri("/api/v1/clients?pageSize=101", UriKind.Relative), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("pageSize");
    }

    private static async Task<PagedResponse<ClientResponse>> GetPageAsync(HttpClient client, string query)
    {
        using var response = await client.GetAsync(new Uri($"/api/v1/clients{query}", UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<ClientResponse>>(TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }

    private async Task ArchiveAsync(Guid clientId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var client = await dbContext.Clients.SingleAsync(item => item.Id == clientId, TestContext.Current.CancellationToken);
        client.Archive(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
