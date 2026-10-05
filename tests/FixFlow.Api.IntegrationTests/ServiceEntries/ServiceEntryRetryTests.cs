using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Features.Parts;
using FixFlow.Api.Features.ServiceEntries;
using FixFlow.Api.Features.ServiceEntries.AddServiceEntry;
using FixFlow.Api.IntegrationTests.Parts;
using FixFlow.Api.IntegrationTests.WorkOrders;

namespace FixFlow.Api.IntegrationTests.ServiceEntries;

public sealed class ServiceEntryRetryTests(FixFlowApiFactory factory) : ServiceEntryTestBase(factory)
{
    [Fact]
    public async Task Should_Add_One_Entry_And_Take_Parts_From_Stock_Once_When_Entry_Is_Sent_Twice()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var part = await scenario.DispatcherClient.CreatePartAsync(PartRequests.NewPart() with { StockQuantity = 10 });
        var request = ServiceEntryRequests.WorkEntry(scenario.WorkOrder, new ServiceEntryPartRequest(part.Id, 3)) with { Id = Guid.CreateVersion7() };

        using var firstResponse = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request);
        using var secondResponse = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadEntryAsync(secondResponse)).Id.ShouldBe(request.Id.ShouldNotBeNull());
        (await scenario.DispatcherClient.ListServiceEntriesAsync(scenario.WorkOrder.Id)).ShouldHaveSingleItem().Id.ShouldBe(request.Id.Value);
        (await GetStockQuantityAsync(scenario.DispatcherClient, part.Id)).ShouldBe(7);
    }

    [Fact]
    public async Task Should_Return_Stored_Entry_When_Entry_Is_Sent_Again_After_Work_Order_Was_Completed()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var request = ServiceEntryRequests.WorkEntry(scenario.WorkOrder) with { Id = Guid.CreateVersion7() };
        await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, request);
        using var completeResponse = await scenario.TechnicianClient.PostTransitionAsync(scenario.WorkOrder.Id, "complete");
        completeResponse.EnsureSuccessStatusCode();

        using var response = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadEntryAsync(response)).Id.ShouldBe(request.Id.ShouldNotBeNull());
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Identifier_Is_Used_On_Another_Work_Order()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var request = ServiceEntryRequests.WorkEntry(scenario.WorkOrder) with { Id = Guid.CreateVersion7() };
        await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, request);
        var otherWorkOrder = await scenario.DispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(scenario.WorkOrder.DeviceId));
        await Factory.AssignTechnicianDirectlyAsync(otherWorkOrder.Id, scenario.WorkOrder.TechnicianId.ShouldNotBeNull());

        using var response = await scenario.TechnicianClient.PostServiceEntryAsync(otherWorkOrder.Id, request);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, ServiceEntryErrors.IdConflict.Code);
    }

    [Fact]
    public async Task Should_Add_One_Entry_And_Take_Parts_From_Stock_Once_When_Entry_Is_Sent_Twice_Concurrently()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var part = await scenario.DispatcherClient.CreatePartAsync(PartRequests.NewPart() with { StockQuantity = 10 });
        var request = ServiceEntryRequests.WorkEntry(scenario.WorkOrder, new ServiceEntryPartRequest(part.Id, 3)) with { Id = Guid.CreateVersion7() };

        var responses = await Task.WhenAll(
            scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request),
            scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request));

        try
        {
            responses.Select(response => response.StatusCode).ShouldBe([HttpStatusCode.Created, HttpStatusCode.OK], ignoreOrder: true);
            (await scenario.DispatcherClient.ListServiceEntriesAsync(scenario.WorkOrder.Id)).ShouldHaveSingleItem().Id.ShouldBe(request.Id.ShouldNotBeNull());
            (await GetStockQuantityAsync(scenario.DispatcherClient, part.Id)).ShouldBe(7);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    private static async Task<ServiceEntryResponse> ReadEntryAsync(HttpResponseMessage response)
    {
        var entry = await response.Content.ReadFromJsonAsync<ServiceEntryResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        return entry.ShouldNotBeNull();
    }

    private static async Task<int> GetStockQuantityAsync(HttpClient client, Guid partId)
    {
        var part = await client.GetFromJsonAsync<PartResponse>(PartRequests.PartUri(partId), TestContext.Current.CancellationToken);
        return part.ShouldNotBeNull().StockQuantity;
    }
}
