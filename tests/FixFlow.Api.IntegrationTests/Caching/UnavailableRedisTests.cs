using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Devices;
using FixFlow.Api.IntegrationTests.Auth;
using FixFlow.Api.IntegrationTests.Clients;
using FixFlow.Api.IntegrationTests.Devices;
using Microsoft.AspNetCore.Hosting;

namespace FixFlow.Api.IntegrationTests.Caching;

public sealed class UnavailableRedisTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private const string UnreachableRedisConnectionString = "127.0.0.1:1";

    [Fact]
    public async Task Should_Serve_And_Refresh_Lists_When_Configured_Redis_Is_Unavailable()
    {
        await using var factoryWithoutRedis = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Redis", UnreachableRedisConnectionString));
        using var client = factoryWithoutRedis.CreateClient();
        var tokens = await client.LoginAsync(await CreateUserAsync(Roles.Dispatcher));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var stopwatch = Stopwatch.StartNew();

        var owner = await client.CreateClientAsync(ClientRequests.NewClient("Alfa"));
        var clientsBeforeChange = await GetPageAsync<ClientResponse>(client, ClientRequests.ClientsUri);
        await client.CreateClientAsync(ClientRequests.NewClient("Bravo"));
        var clientsAfterChange = await GetPageAsync<ClientResponse>(client, ClientRequests.ClientsUri);
        await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id));
        var devices = await GetPageAsync<DeviceResponse>(client, DeviceRequests.DevicesUri);

        clientsBeforeChange.TotalCount.ShouldBe(1);
        clientsAfterChange.Items.Select(item => item.Name).ShouldBe(["Alfa", "Bravo"]);
        devices.TotalCount.ShouldBe(1);
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(15));
    }

    private static async Task<PagedResponse<T>> GetPageAsync<T>(HttpClient client, Uri uri)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<T>>(uri, TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }
}
