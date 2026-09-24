using System.Net.Http.Json;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Devices;
using FixFlow.Api.Features.Devices.UpdateDevice;
using FixFlow.Api.IntegrationTests.Clients;
using FixFlow.Api.IntegrationTests.Devices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Caching;

public sealed class ListCacheTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Store_Client_List_In_Redis_When_List_Is_Requested()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreateClientAsync(ClientRequests.NewClient());

        await GetClientsAsync(client);

        var distributedCache = Factory.Services.GetRequiredService<IDistributedCache>();
        var cachedList = await distributedCache.GetAsync($"clients:list:1:{PagedRequest.DefaultPageSize}:", TestContext.Current.CancellationToken);
        cachedList.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Return_New_Client_When_Client_Is_Created_After_List_Was_Cached()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        await client.CreateClientAsync(ClientRequests.NewClient("Alfa"));
        await GetClientsAsync(client);

        await client.CreateClientAsync(ClientRequests.NewClient("Bravo"));

        var page = await GetClientsAsync(client);
        page.Items.Select(item => item.Name).ShouldBe(["Alfa", "Bravo"]);
    }

    [Fact]
    public async Task Should_Return_Updated_Client_When_Client_Is_Updated_After_List_Was_Cached()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdClient = await client.CreateClientAsync(ClientRequests.NewClient("Alfa"));
        await GetClientsAsync(client);

        using var response = await client.PutAsJsonAsync(ClientRequests.ClientUri(createdClient.Id), ClientRequests.NewClient("Alfa Serwis"), TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var page = await GetClientsAsync(client);
        page.Items.Select(item => item.Name).ShouldBe(["Alfa Serwis"]);
    }

    [Fact]
    public async Task Should_Not_Return_Devices_Of_Archived_Client_When_Device_List_Was_Cached()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id));
        (await GetDevicesAsync(client)).TotalCount.ShouldBe(1);

        using var response = await client.PostAsync(new Uri($"/api/v1/clients/{owner.Id}/archive", UriKind.Relative), null, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        (await GetDevicesAsync(client)).TotalCount.ShouldBe(0);
        (await GetClientsAsync(client)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_New_And_Updated_Devices_When_Devices_Change_After_List_Was_Cached()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        var device = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-1"));
        await GetDevicesAsync(client);

        await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-2"));
        var afterCreate = await GetDevicesAsync(client);
        var updatedDevice = new UpdateDeviceRequest("SN-3", device.Model, device.Manufacturer, device.InstallationDate);
        using var updateResponse = await client.PutAsJsonAsync(DeviceRequests.DeviceUri(device.Id), updatedDevice, TestContext.Current.CancellationToken);
        var afterUpdate = await GetDevicesAsync(client);

        afterCreate.Items.Select(item => item.SerialNumber).ShouldBe(["SN-1", "SN-2"]);
        updateResponse.EnsureSuccessStatusCode();
        afterUpdate.Items.Select(item => item.SerialNumber).ShouldBe(["SN-2", "SN-3"]);
    }

    private static async Task<PagedResponse<ClientResponse>> GetClientsAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<ClientResponse>>(ClientRequests.ClientsUri, TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }

    private static async Task<PagedResponse<DeviceResponse>> GetDevicesAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<DeviceResponse>>(DeviceRequests.DevicesUri, TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }
}
