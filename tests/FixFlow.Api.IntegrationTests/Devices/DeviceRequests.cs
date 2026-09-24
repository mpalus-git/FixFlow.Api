using System.Net.Http.Json;
using FixFlow.Api.Features.Devices;
using FixFlow.Api.Features.Devices.CreateDevice;

namespace FixFlow.Api.IntegrationTests.Devices;

public static class DeviceRequests
{
    public static readonly Uri DevicesUri = new("/api/v1/devices", UriKind.Relative);

    public static Uri DeviceUri(Guid deviceId) => new($"/api/v1/devices/{deviceId}", UriKind.Relative);

    public static CreateDeviceRequest NewDevice(Guid clientId, string serialNumber = "AC-1001") =>
        new(clientId, serialNumber, "Split 3.5 kW", "Daikin", new DateOnly(2024, 5, 20));

    public static async Task<DeviceResponse> CreateDeviceAsync(this HttpClient client, CreateDeviceRequest request)
    {
        using var response = await client.PostAsJsonAsync(DevicesUri, request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var createdDevice = await response.Content.ReadFromJsonAsync<DeviceResponse>(TestContext.Current.CancellationToken);
        return createdDevice.ShouldNotBeNull();
    }
}
