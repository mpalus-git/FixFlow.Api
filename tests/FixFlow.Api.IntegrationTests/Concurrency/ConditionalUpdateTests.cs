using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Clients.UpdateClient;
using FixFlow.Api.IntegrationTests.Clients;

namespace FixFlow.Api.IntegrationTests.Concurrency;

public sealed class ConditionalUpdateTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Dictionary<string, Func<HttpClient, Task<UpdatableResource>>> Resources = new()
    {
        ["client"] = async client =>
        {
            var createdClient = await client.CreateClientAsync(ClientRequests.NewClient());
            return new UpdatableResource(
                ClientRequests.ClientUri(createdClient.Id),
                new UpdateClientRequest("Klimat-Serwis Sp. z o.o.", new ClientAddress("Przemysłowa", "3", "30-701", "Kraków"), "Jan Kowalski", "+48 600 300 400"));
        },
    };

    public static TheoryData<string> ResourceNames { get; } = [.. Resources.Keys];

    [Theory]
    [MemberData(nameof(ResourceNames))]
    public async Task Should_Return_Precondition_Required_When_Update_Has_No_If_Match(string resourceName)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var resource = await Resources[resourceName](client);
        using var content = JsonContent.Create(resource.Update, resource.Update.GetType(), options: ApiJson.Options);

        using var response = await client.PutAsync(resource.Uri, content, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.PreconditionRequired, PreconditionErrors.Required.Code);
    }

    [Theory]
    [MemberData(nameof(ResourceNames))]
    public async Task Should_Update_And_Return_New_ETag_When_If_Match_Is_Current(string resourceName)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var resource = await Resources[resourceName](client);
        var readETag = await client.GetETagAsync(resource.Uri, TestContext.Current.CancellationToken);

        using var response = await client.PutWithIfMatchAsync(resource.Uri, resource.Update, readETag, ApiJson.Options, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.ETag().ShouldNotBe(readETag);
        (await client.GetETagAsync(resource.Uri, TestContext.Current.CancellationToken)).ShouldBe(response.ETag());
    }

    [Theory]
    [MemberData(nameof(ResourceNames))]
    public async Task Should_Return_Precondition_Failed_And_Keep_Resource_When_If_Match_Is_Stale(string resourceName)
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var resource = await Resources[resourceName](client);
        var staleETag = await client.GetETagAsync(resource.Uri, TestContext.Current.CancellationToken);
        using var firstUpdate = await client.PutWithIfMatchAsync(resource.Uri, resource.Update, staleETag, ApiJson.Options, TestContext.Current.CancellationToken);

        using var response = await client.PutWithIfMatchAsync(resource.Uri, resource.Update, staleETag, ApiJson.Options, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.PreconditionFailed, PreconditionErrors.Failed.Code);
        (await client.GetETagAsync(resource.Uri, TestContext.Current.CancellationToken)).ShouldBe(firstUpdate.ETag());
    }

    private sealed record UpdatableResource(Uri Uri, object Update);
}
