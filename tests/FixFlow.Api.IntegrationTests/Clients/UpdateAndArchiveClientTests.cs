using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Clients.UpdateClient;
using FixFlow.Api.Features.Devices;
using FixFlow.Api.IntegrationTests.Devices;

namespace FixFlow.Api.IntegrationTests.Clients;

public sealed class UpdateAndArchiveClientTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly UpdateClientRequest UpdatedDetails = new(
        "Klimat-Serwis Sp. z o.o.",
        new ClientAddress("Przemysłowa", "3/1", "30-701", "Kraków"),
        "Jan Kowalski",
        "+48 600 300 400");

    [Fact]
    public async Task Should_Replace_Client_Details_When_Dispatcher_Updates_Active_Client()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdClient = await client.CreateClientAsync(ClientRequests.NewClient());

        using var response = await client.PutWithCurrentETagAsync(ClientRequests.ClientUri(createdClient.Id), UpdatedDetails, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var storedClient = await client.GetFromJsonAsync<ClientResponse>(ClientRequests.ClientUri(createdClient.Id), TestContext.Current.CancellationToken);
        storedClient.ShouldNotBeNull();
        storedClient.Name.ShouldBe(UpdatedDetails.Name);
        storedClient.Address.ShouldBe(UpdatedDetails.Address);
        storedClient.ContactPerson.ShouldBe(UpdatedDetails.ContactPerson);
        storedClient.Email.ShouldBeNull();
        storedClient.CreatedAt.ShouldBe(createdClient.CreatedAt);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Archived_Client_Is_Updated()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdClient = await client.CreateClientAsync(ClientRequests.NewClient());
        using var archiveResponse = await client.PostAsync(ArchiveUri(createdClient.Id), null, TestContext.Current.CancellationToken);

        using var response = await client.PutWithCurrentETagAsync(ClientRequests.ClientUri(createdClient.Id), UpdatedDetails, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, ClientErrors.Archived.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Update_Has_Invalid_Phone()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdClient = await client.CreateClientAsync(ClientRequests.NewClient());

        using var response = await client.PutWithCurrentETagAsync(ClientRequests.ClientUri(createdClient.Id), UpdatedDetails with { Phone = "123" }, TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("phone");
    }

    [Fact]
    public async Task Should_Keep_Client_Available_By_Id_With_First_Archive_Time_When_Archived_Twice()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdClient = await client.CreateClientAsync(ClientRequests.NewClient());

        using var firstResponse = await client.PostAsync(ArchiveUri(createdClient.Id), null, TestContext.Current.CancellationToken);
        var afterFirstArchive = await client.GetFromJsonAsync<ClientResponse>(ClientRequests.ClientUri(createdClient.Id), TestContext.Current.CancellationToken);
        using var secondResponse = await client.PostAsync(ArchiveUri(createdClient.Id), null, TestContext.Current.CancellationToken);
        var afterSecondArchive = await client.GetFromJsonAsync<ClientResponse>(ClientRequests.ClientUri(createdClient.Id), TestContext.Current.CancellationToken);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        afterFirstArchive.ShouldNotBeNull();
        afterFirstArchive.ArchivedAt.ShouldNotBeNull();
        afterSecondArchive.ShouldNotBeNull();
        afterSecondArchive.ArchivedAt.ShouldBe(afterFirstArchive.ArchivedAt);
    }

    [Fact]
    public async Task Should_Archive_Active_Devices_Of_Client_When_Client_Is_Archived()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var archivedClient = await client.CreateClientAsync(ClientRequests.NewClient("Archived client"));
        var otherClient = await client.CreateClientAsync(ClientRequests.NewClient("Other client"));
        var firstDevice = await client.CreateDeviceAsync(DeviceRequests.NewDevice(archivedClient.Id, "SN-1"));
        var secondDevice = await client.CreateDeviceAsync(DeviceRequests.NewDevice(archivedClient.Id, "SN-2"));
        var otherDevice = await client.CreateDeviceAsync(DeviceRequests.NewDevice(otherClient.Id, "SN-3"));

        using var response = await client.PostAsync(ArchiveUri(archivedClient.Id), null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var storedClient = await client.GetFromJsonAsync<ClientResponse>(ClientRequests.ClientUri(archivedClient.Id), TestContext.Current.CancellationToken);
        storedClient.ShouldNotBeNull();
        foreach (var deviceId in new[] { firstDevice.Id, secondDevice.Id })
        {
            var storedDevice = await client.GetFromJsonAsync<DeviceResponse>(DeviceRequests.DeviceUri(deviceId), TestContext.Current.CancellationToken);
            storedDevice.ShouldNotBeNull();
            storedDevice.ArchivedAt.ShouldBe(storedClient.ArchivedAt);
        }

        var storedOtherDevice = await client.GetFromJsonAsync<DeviceResponse>(DeviceRequests.DeviceUri(otherDevice.Id), TestContext.Current.CancellationToken);
        storedOtherDevice.ShouldNotBeNull();
        storedOtherDevice.ArchivedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Keep_Earlier_Archive_Time_Of_Device_When_Its_Client_Is_Archived_Later()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdClient = await client.CreateClientAsync(ClientRequests.NewClient());
        var device = await client.CreateDeviceAsync(DeviceRequests.NewDevice(createdClient.Id));
        using var deviceArchiveResponse = await client.PostAsync(new Uri($"/api/v1/devices/{device.Id}/archive", UriKind.Relative), null, TestContext.Current.CancellationToken);
        var archivedDevice = await client.GetFromJsonAsync<DeviceResponse>(DeviceRequests.DeviceUri(device.Id), TestContext.Current.CancellationToken);

        using var clientArchiveResponse = await client.PostAsync(ArchiveUri(createdClient.Id), null, TestContext.Current.CancellationToken);

        var storedDevice = await client.GetFromJsonAsync<DeviceResponse>(DeviceRequests.DeviceUri(device.Id), TestContext.Current.CancellationToken);
        archivedDevice.ShouldNotBeNull();
        storedDevice.ShouldNotBeNull();
        clientArchiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        storedDevice.ArchivedAt.ShouldBe(archivedDevice.ArchivedAt);
    }

    private static Uri ArchiveUri(Guid clientId) => new($"/api/v1/clients/{clientId}/archive", UriKind.Relative);
}
