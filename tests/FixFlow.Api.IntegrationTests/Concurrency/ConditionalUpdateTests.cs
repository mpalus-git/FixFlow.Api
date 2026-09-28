using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Clients.UpdateClient;
using FixFlow.Api.Features.Devices.UpdateDevice;
using FixFlow.Api.Features.Parts.UpdatePart;
using FixFlow.Api.Features.WorkOrders;
using FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;
using FixFlow.Api.IntegrationTests.Clients;
using FixFlow.Api.IntegrationTests.Devices;
using FixFlow.Api.IntegrationTests.Parts;
using FixFlow.Api.IntegrationTests.WorkOrders;

namespace FixFlow.Api.IntegrationTests.Concurrency;

public sealed class ConditionalUpdateTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly UpdateWorkOrderRequest ChangedWorkOrder = new("Leak and noisy fan", WorkOrderPriority.High, DateTimeOffset.UtcNow.AddDays(10));

    private static readonly Dictionary<string, Func<HttpClient, Task<UpdatableResource>>> Resources = new()
    {
        ["client"] = async client =>
        {
            var createdClient = await client.CreateClientAsync(ClientRequests.NewClient());
            return new UpdatableResource(
                ClientRequests.ClientUri(createdClient.Id),
                new UpdateClientRequest("Klimat-Serwis Sp. z o.o.", new ClientAddress("Przemysłowa", "3", "30-701", "Kraków"), "Jan Kowalski", "+48 600 300 400"));
        },
        ["device"] = async client =>
        {
            var owner = await client.CreateClientAsync(ClientRequests.NewClient());
            var device = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id));
            return new UpdatableResource(
                DeviceRequests.DeviceUri(device.Id),
                new UpdateDeviceRequest("AC-2002", "Multi 5 kW", "Mitsubishi", new DateOnly(2025, 3, 10)));
        },
        ["part"] = async client =>
        {
            var part = await client.CreatePartAsync(PartRequests.NewPart());
            return new UpdatableResource(PartRequests.PartUri(part.Id), new UpdatePartRequest("Filtr węglowy", "FLT-200", 59.50m));
        },
        ["work order"] = async client =>
        {
            var workOrder = await CreateWorkOrderAsync(client);
            return new UpdatableResource(WorkOrderRequests.WorkOrderUri(workOrder.Id), ChangedWorkOrder);
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

    [Fact]
    public async Task Should_Accept_Update_When_If_Match_Carries_ETag_Returned_By_Assignment()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await CreateWorkOrderAsync(client);
        var technician = await CreateUserAsync(Roles.Technician);
        using var assignResponse = await client.PostAssignAsync(workOrder.Id, technician.Id);

        using var response = await client.PutWithIfMatchAsync(
            WorkOrderRequests.WorkOrderUri(workOrder.Id),
            ChangedWorkOrder,
            assignResponse.ETag(),
            ApiJson.Options,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<WorkOrderResponse> CreateWorkOrderAsync(HttpClient client) =>
        await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await client.CreateServicedDeviceAsync()));

    private sealed record UpdatableResource(Uri Uri, object Update);
}
