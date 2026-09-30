using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Devices.ListDevices;
using FixFlow.Api.IntegrationTests.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Devices;

public sealed class ListDevicesTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Requested_Page_Ordered_By_Serial_Number_With_Total_Count_When_Devices_Exceed_Page_Size()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        foreach (var serialNumber in new[] { "SN-4", "SN-1", "SN-5", "SN-3", "SN-2" })
        {
            await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, serialNumber));
        }

        var page = await GetPageAsync(client, "?page=2&pageSize=2");

        page.Items.Select(item => item.SerialNumber).ShouldBe(["SN-3", "SN-4"]);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(2);
        page.TotalCount.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Return_Only_Devices_Of_Given_Client_When_Client_Filter_Is_Provided()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var firstOwner = await client.CreateClientAsync(ClientRequests.NewClient("Alfa"));
        var secondOwner = await client.CreateClientAsync(ClientRequests.NewClient("Bravo"));
        await client.CreateDeviceAsync(DeviceRequests.NewDevice(firstOwner.Id, "SN-1"));
        await client.CreateDeviceAsync(DeviceRequests.NewDevice(secondOwner.Id, "SN-2"));

        var page = await GetPageAsync(client, $"?clientId={secondOwner.Id}");

        page.Items.Select(item => item.SerialNumber).ShouldBe(["SN-2"]);
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Include_Client_Name_When_Devices_Are_Listed()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var firstOwner = await client.CreateClientAsync(ClientRequests.NewClient("Alfa"));
        var secondOwner = await client.CreateClientAsync(ClientRequests.NewClient("Bravo"));
        var firstDevice = await client.CreateDeviceAsync(DeviceRequests.NewDevice(firstOwner.Id, "SN-1"));
        var secondDevice = await client.CreateDeviceAsync(DeviceRequests.NewDevice(secondOwner.Id, "SN-2"));

        var page = await GetPageAsync(client, string.Empty);

        page.Items.ShouldBe(
        [
            new DeviceListItemResponse(firstDevice.Id, firstOwner.Id, "Alfa", firstDevice.SerialNumber, firstDevice.Model, firstDevice.Manufacturer, firstDevice.InstallationDate, firstDevice.CreatedAt, null),
            new DeviceListItemResponse(secondDevice.Id, secondOwner.Id, "Bravo", secondDevice.SerialNumber, secondDevice.Model, secondDevice.Manufacturer, secondDevice.InstallationDate, secondDevice.CreatedAt, null),
        ]);
    }

    [Theory]
    [InlineData("sn-2")]
    [InlineData("MULTI")]
    [InlineData("mitsu")]
    public async Task Should_Return_Devices_Matching_Serial_Number_Model_Or_Manufacturer_When_Search_Is_Provided(string search)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-1"));
        await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-2") with { Model = "Multi 5 kW", Manufacturer = "Mitsubishi" });

        var page = await GetPageAsync(client, $"?search={search}");

        page.Items.Select(item => item.SerialNumber).ShouldBe(["SN-2"]);
    }

    [Fact]
    public async Task Should_Not_List_Archived_Devices_When_Listing()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-ACTIVE"));
        var archivedDevice = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "SN-ARCHIVED"));
        await ArchiveAsync(archivedDevice.Id);

        var page = await GetPageAsync(client, string.Empty);

        page.Items.Select(item => item.SerialNumber).ShouldBe(["SN-ACTIVE"]);
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Allow_Technician_To_List_Devices()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await dispatcherClient.CreateClientAsync(ClientRequests.NewClient());
        await dispatcherClient.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id));
        using var technicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        var page = await GetPageAsync(technicianClient, string.Empty);

        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Search_Is_Longer_Than_One_Hundred_Characters()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await client.GetAsync(new Uri($"/api/v1/devices?search={new string('a', 101)}", UriKind.Relative), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("search");
    }

    private static async Task<PagedResponse<DeviceListItemResponse>> GetPageAsync(HttpClient client, string query)
    {
        using var response = await client.GetAsync(new Uri($"/api/v1/devices{query}", UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<DeviceListItemResponse>>(TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }

    private async Task ArchiveAsync(Guid deviceId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var device = await dbContext.Devices.SingleAsync(item => item.Id == deviceId, TestContext.Current.CancellationToken);
        device.Archive(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
