using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Clients.UpdateClient;

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

        using var response = await client.PutAsJsonAsync(ClientRequests.ClientUri(createdClient.Id), UpdatedDetails, TestContext.Current.CancellationToken);

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

        using var response = await client.PutAsJsonAsync(ClientRequests.ClientUri(createdClient.Id), UpdatedDetails, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, ClientErrors.Archived.Code);
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Updated_Client_Does_Not_Exist()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Admin);

        using var response = await client.PutAsJsonAsync(ClientRequests.ClientUri(Guid.CreateVersion7()), UpdatedDetails, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, ClientErrors.NotFound.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Update_Has_Invalid_Phone()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdClient = await client.CreateClientAsync(ClientRequests.NewClient());

        using var response = await client.PutAsJsonAsync(ClientRequests.ClientUri(createdClient.Id), UpdatedDetails with { Phone = "123" }, TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync(nameof(UpdateClientRequest.Phone));
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_Technician_Updates_Or_Archives_Client()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var createdClient = await dispatcherClient.CreateClientAsync(ClientRequests.NewClient());
        using var technicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var updateResponse = await technicianClient.PutAsJsonAsync(ClientRequests.ClientUri(createdClient.Id), UpdatedDetails, TestContext.Current.CancellationToken);
        using var archiveResponse = await technicianClient.PostAsync(ArchiveUri(createdClient.Id), null, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
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
    public async Task Should_Return_Not_Found_Problem_When_Archived_Client_Does_Not_Exist()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await client.PostAsync(ArchiveUri(Guid.CreateVersion7()), null, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, ClientErrors.NotFound.Code);
    }

    private static Uri ArchiveUri(Guid clientId) => new($"/api/v1/clients/{clientId}/archive", UriKind.Relative);
}
