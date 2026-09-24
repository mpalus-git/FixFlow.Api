using System.Net.Http.Json;
using FixFlow.Api.Features.Parts;
using FixFlow.Api.Features.Parts.CreatePart;

namespace FixFlow.Api.IntegrationTests.Parts;

public static class PartRequests
{
    public static readonly Uri PartsUri = new("/api/v1/parts", UriKind.Relative);

    public static Uri PartUri(Guid partId) => new($"/api/v1/parts/{partId}", UriKind.Relative);

    public static Uri ArchivePartUri(Guid partId) => new($"/api/v1/parts/{partId}/archive", UriKind.Relative);

    public static Uri RestockPartUri(Guid partId) => new($"/api/v1/parts/{partId}/restock", UriKind.Relative);

    public static CreatePartRequest NewPart(string catalogNumber = "FLT-100", string name = "Filtr powietrza") =>
        new(name, catalogNumber, 12, 49.99m);

    public static async Task<PartResponse> CreatePartAsync(this HttpClient client, CreatePartRequest request)
    {
        using var response = await client.PostAsJsonAsync(PartsUri, request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var createdPart = await response.Content.ReadFromJsonAsync<PartResponse>(TestContext.Current.CancellationToken);
        return createdPart.ShouldNotBeNull();
    }

    public static async Task ArchivePartAsync(this HttpClient client, Guid partId)
    {
        using var response = await client.PostAsync(ArchivePartUri(partId), null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
