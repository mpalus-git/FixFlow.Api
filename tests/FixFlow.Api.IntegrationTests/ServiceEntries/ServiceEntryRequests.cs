using System.Net.Http.Json;
using FixFlow.Api.Features.ServiceEntries;
using FixFlow.Api.Features.ServiceEntries.AddServiceEntry;
using FixFlow.Api.Features.WorkOrders;

namespace FixFlow.Api.IntegrationTests.ServiceEntries;

public static class ServiceEntryRequests
{
    public static Uri ServiceEntriesUri(Guid workOrderId) => new($"/api/v1/work-orders/{workOrderId}/service-entries", UriKind.Relative);

    public static AddServiceEntryRequest WorkEntry(WorkOrderResponse workOrder, params ServiceEntryPartRequest[] parts) => new(
        "Replaced filters",
        PhotoUrls: ["https://photos.test/1.jpg"],
        WorkStartedAt: workOrder.StartedAt,
        WorkFinishedAt: DateTimeOffset.UtcNow,
        Latitude: 52.2297,
        Longitude: 21.0122,
        Parts: parts);

    public static AddServiceEntryRequest Correction(params ServiceEntryPartRequest[] returnedParts) =>
        new("Parts were not used", IsCorrection: true, Parts: returnedParts);

    public static Task<HttpResponseMessage> PostServiceEntryAsync(this HttpClient client, Guid workOrderId, AddServiceEntryRequest request) =>
        client.PostAsJsonAsync(ServiceEntriesUri(workOrderId), request, ApiJson.Options, TestContext.Current.CancellationToken);

    public static async Task<ServiceEntryResponse> AddServiceEntryAsync(this HttpClient client, Guid workOrderId, AddServiceEntryRequest request)
    {
        using var response = await client.PostServiceEntryAsync(workOrderId, request);
        response.EnsureSuccessStatusCode();
        var entry = await response.Content.ReadFromJsonAsync<ServiceEntryResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        return entry.ShouldNotBeNull();
    }

    public static async Task<List<ServiceEntryResponse>> ListServiceEntriesAsync(this HttpClient client, Guid workOrderId)
    {
        var entries = await client.GetFromJsonAsync<List<ServiceEntryResponse>>(ServiceEntriesUri(workOrderId), ApiJson.Options, TestContext.Current.CancellationToken);
        return entries.ShouldNotBeNull();
    }
}
