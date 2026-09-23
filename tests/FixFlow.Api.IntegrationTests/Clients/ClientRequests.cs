using System.Net.Http.Json;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Clients.CreateClient;

namespace FixFlow.Api.IntegrationTests.Clients;

public static class ClientRequests
{
    public static readonly Uri ClientsUri = new("/api/v1/clients", UriKind.Relative);

    public static Uri ClientUri(Guid clientId) => new($"/api/v1/clients/{clientId}", UriKind.Relative);

    public static CreateClientRequest NewClient(string name = "Klimat-Serwis") =>
        new(name, new ClientAddress("Marszałkowska", "10A", "00-590", "Warszawa"), "Anna Nowak", "+48 600 100 200", "biuro@klimat.test");

    public static async Task<ClientResponse> CreateClientAsync(this HttpClient client, CreateClientRequest request)
    {
        using var response = await client.PostAsJsonAsync(ClientsUri, request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var createdClient = await response.Content.ReadFromJsonAsync<ClientResponse>(TestContext.Current.CancellationToken);
        return createdClient.ShouldNotBeNull();
    }
}
