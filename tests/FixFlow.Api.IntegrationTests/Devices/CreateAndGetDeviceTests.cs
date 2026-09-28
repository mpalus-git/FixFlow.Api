using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Devices;
using FixFlow.Api.IntegrationTests.Clients;

namespace FixFlow.Api.IntegrationTests.Devices;

public sealed class CreateAndGetDeviceTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData(Roles.Dispatcher)]
    [InlineData(Roles.Admin)]
    public async Task Should_Create_Device_With_Location_When_Dispatcher_Or_Admin_Creates_It(string role)
    {
        using var client = await CreateAuthenticatedClientAsync(role);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        var request = DeviceRequests.NewDevice(owner.Id);

        using var response = await client.PostAsJsonAsync(DeviceRequests.DevicesUri, request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdDevice = await response.Content.ReadFromJsonAsync<DeviceResponse>(TestContext.Current.CancellationToken);
        createdDevice.ShouldNotBeNull();
        response.Headers.Location.ShouldBe(DeviceRequests.DeviceUri(createdDevice.Id));
        createdDevice.ClientId.ShouldBe(owner.Id);
        createdDevice.InstallationDate.ShouldBe(request.InstallationDate);
        createdDevice.ArchivedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Store_Trimmed_Upper_Case_Serial_Number_When_Device_Is_Created()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());

        var createdDevice = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "  ac-1001x "));

        createdDevice.SerialNumber.ShouldBe("AC-1001X");
    }

    [Theory]
    [InlineData("AC-1001")]
    [InlineData(" ac-1001 ")]
    public async Task Should_Return_Conflict_Problem_When_Serial_Number_Is_Already_Used(string duplicateSerialNumber)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, "AC-1001"));

        using var response = await client.PostAsJsonAsync(DeviceRequests.DevicesUri, DeviceRequests.NewDevice(owner.Id, duplicateSerialNumber), TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, DeviceErrors.DuplicateSerialNumber.Code);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Client_Of_New_Device_Is_Archived()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        using var archiveResponse = await client.PostAsync(new Uri($"/api/v1/clients/{owner.Id}/archive", UriKind.Relative), null, TestContext.Current.CancellationToken);

        using var response = await client.PostAsJsonAsync(DeviceRequests.DevicesUri, DeviceRequests.NewDevice(owner.Id), TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, DeviceErrors.ClientArchived.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Installation_Date_Is_In_The_Future()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        var request = DeviceRequests.NewDevice(owner.Id) with { InstallationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2) };

        using var response = await client.PostAsJsonAsync(DeviceRequests.DevicesUri, request, TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("installationDate");
    }

    [Fact]
    public async Task Should_Return_Device_When_Technician_Gets_Existing_Device()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var owner = await dispatcherClient.CreateClientAsync(ClientRequests.NewClient());
        var createdDevice = await dispatcherClient.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id));
        using var technicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        var fetchedDevice = await technicianClient.GetFromJsonAsync<DeviceResponse>(DeviceRequests.DeviceUri(createdDevice.Id), TestContext.Current.CancellationToken);

        fetchedDevice.ShouldBe(createdDevice);
    }
}
