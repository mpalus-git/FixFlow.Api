using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Devices;
using FixFlow.Api.Features.Devices.UpdateDevice;
using FixFlow.Api.IntegrationTests.Clients;

namespace FixFlow.Api.IntegrationTests.Devices;

public sealed class UpdateAndArchiveDeviceTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly UpdateDeviceRequest UpdatedDetails = new(" ac-2002 ", "Multi 5 kW", "Mitsubishi", new DateOnly(2025, 3, 10));

    [Fact]
    public async Task Should_Replace_Device_Details_And_Keep_Client_When_Dispatcher_Updates_Active_Device()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var device = await CreateDeviceAsync(client, "AC-1001");

        using var response = await client.PutAsJsonAsync(DeviceRequests.DeviceUri(device.Id), UpdatedDetails, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var storedDevice = await client.GetFromJsonAsync<DeviceResponse>(DeviceRequests.DeviceUri(device.Id), TestContext.Current.CancellationToken);
        storedDevice.ShouldNotBeNull();
        storedDevice.SerialNumber.ShouldBe("AC-2002");
        storedDevice.Model.ShouldBe(UpdatedDetails.Model);
        storedDevice.Manufacturer.ShouldBe(UpdatedDetails.Manufacturer);
        storedDevice.InstallationDate.ShouldBe(UpdatedDetails.InstallationDate);
        storedDevice.ClientId.ShouldBe(device.ClientId);
        storedDevice.CreatedAt.ShouldBe(device.CreatedAt);
    }

    [Fact]
    public async Task Should_Accept_Update_When_Device_Keeps_Its_Own_Serial_Number_In_Different_Case()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var device = await CreateDeviceAsync(client, "AC-1001");

        using var response = await client.PutAsJsonAsync(DeviceRequests.DeviceUri(device.Id), UpdatedDetails with { SerialNumber = "ac-1001" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Update_Uses_Serial_Number_Of_Another_Device()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "AC-2002"));
        var device = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "AC-1001"));

        using var response = await client.PutAsJsonAsync(DeviceRequests.DeviceUri(device.Id), UpdatedDetails, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, DeviceErrors.DuplicateSerialNumber.Code);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Archived_Device_Is_Updated()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var device = await CreateDeviceAsync(client, "AC-1001");
        using var archiveResponse = await client.PostAsync(ArchiveUri(device.Id), null, TestContext.Current.CancellationToken);

        using var response = await client.PutAsJsonAsync(DeviceRequests.DeviceUri(device.Id), UpdatedDetails, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, DeviceErrors.Archived.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Update_Has_Future_Installation_Date()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var device = await CreateDeviceAsync(client, "AC-1001");
        var request = UpdatedDetails with { InstallationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2) };

        using var response = await client.PutAsJsonAsync(DeviceRequests.DeviceUri(device.Id), request, TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("installationDate");
    }

    [Fact]
    public async Task Should_Keep_Device_Available_By_Id_With_First_Archive_Time_When_Archived_Twice()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var device = await CreateDeviceAsync(client, "AC-1001");

        using var firstResponse = await client.PostAsync(ArchiveUri(device.Id), null, TestContext.Current.CancellationToken);
        var afterFirstArchive = await client.GetFromJsonAsync<DeviceResponse>(DeviceRequests.DeviceUri(device.Id), TestContext.Current.CancellationToken);
        using var secondResponse = await client.PostAsync(ArchiveUri(device.Id), null, TestContext.Current.CancellationToken);
        var afterSecondArchive = await client.GetFromJsonAsync<DeviceResponse>(DeviceRequests.DeviceUri(device.Id), TestContext.Current.CancellationToken);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        afterFirstArchive.ShouldNotBeNull();
        afterFirstArchive.ArchivedAt.ShouldNotBeNull();
        afterSecondArchive.ShouldNotBeNull();
        afterSecondArchive.ArchivedAt.ShouldBe(afterFirstArchive.ArchivedAt);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Serial_Number_Of_Archived_Device_Is_Reused()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var device = await CreateDeviceAsync(client, "AC-1001");
        using var archiveResponse = await client.PostAsync(ArchiveUri(device.Id), null, TestContext.Current.CancellationToken);

        using var response = await client.PostAsJsonAsync(DeviceRequests.DevicesUri, DeviceRequests.NewDevice(device.ClientId, "AC-1001"), TestContext.Current.CancellationToken);

        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, DeviceErrors.DuplicateSerialNumber.Code);
    }

    private static async Task<DeviceResponse> CreateDeviceAsync(HttpClient client, string serialNumber)
    {
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        return await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, serialNumber));
    }

    private static Uri ArchiveUri(Guid deviceId) => new($"/api/v1/devices/{deviceId}/archive", UriKind.Relative);
}
