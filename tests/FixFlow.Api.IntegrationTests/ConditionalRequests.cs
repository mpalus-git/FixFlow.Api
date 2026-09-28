using System.Net.Http.Json;
using System.Text.Json;

namespace FixFlow.Api.IntegrationTests;

public static class ConditionalRequests
{
    public const string AnyVersion = "*";

    public static Task<HttpResponseMessage> PutWithCurrentETagAsync<TBody>(this HttpClient client, Uri uri, TBody body, CancellationToken cancellationToken) =>
        client.PutWithCurrentETagAsync(uri, body, ApiJson.Options, cancellationToken);

    public static async Task<HttpResponseMessage> PutWithCurrentETagAsync<TBody>(
        this HttpClient client,
        Uri uri,
        TBody body,
        JsonSerializerOptions options,
        CancellationToken cancellationToken) =>
        await client.PutWithIfMatchAsync(uri, body, await client.GetETagAsync(uri, cancellationToken), options, cancellationToken);

    public static async Task<HttpResponseMessage> PutWithIfMatchAsync<TBody>(
        this HttpClient client,
        Uri uri,
        TBody body,
        string ifMatch,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, uri) { Content = JsonContent.Create(body, options: options) };
        request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        return await client.SendAsync(request, cancellationToken);
    }

    public static async Task<string> GetETagAsync(this HttpClient client, Uri uri, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response.ETag();
    }

    public static string ETag(this HttpResponseMessage response) => response.Headers.ETag.ShouldNotBeNull().ToString();
}
