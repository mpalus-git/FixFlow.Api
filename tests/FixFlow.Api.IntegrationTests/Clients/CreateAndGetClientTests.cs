using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Clients;

namespace FixFlow.Api.IntegrationTests.Clients;

public sealed class CreateAndGetClientTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Create_Client_With_Location_When_Dispatcher_Creates_It()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var request = ClientRequests.NewClient();

        using var response = await client.PostAsJsonAsync(ClientRequests.ClientsUri, request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdClient = await response.Content.ReadFromJsonAsync<ClientResponse>(TestContext.Current.CancellationToken);
        createdClient.ShouldNotBeNull();
        response.Headers.Location.ShouldBe(ClientRequests.ClientUri(createdClient.Id));
        response.ETag().ShouldBe(await client.GetETagAsync(ClientRequests.ClientUri(createdClient.Id), TestContext.Current.CancellationToken));
        createdClient.Name.ShouldBe(request.Name);
        createdClient.Address.ShouldBe(request.Address);
        createdClient.ArchivedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Postal_Code_Is_Invalid()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var request = ClientRequests.NewClient() with { Address = new ClientAddress("Marszałkowska", "10A", "00590", "Warszawa") };

        using var response = await client.PostAsJsonAsync(ClientRequests.ClientsUri, request, TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("address.postalCode");
    }

    [Theory]
    [InlineData(Roles.Technician)]
    [InlineData(Roles.Dispatcher)]
    public async Task Should_Return_Client_When_Authenticated_User_Gets_Existing_Client(string role)
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdClient = await dispatcherClient.CreateClientAsync(ClientRequests.NewClient());
        using var client = await CreateAuthenticatedClientAsync(role);

        var fetchedClient = await client.GetFromJsonAsync<ClientResponse>(ClientRequests.ClientUri(createdClient.Id), TestContext.Current.CancellationToken);

        fetchedClient.ShouldBe(createdClient);
    }
}
