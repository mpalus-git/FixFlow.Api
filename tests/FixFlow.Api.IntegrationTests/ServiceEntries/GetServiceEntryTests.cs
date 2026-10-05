using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Features.ServiceEntries;
using FixFlow.Api.Features.ServiceEntries.AddServiceEntry;
using FixFlow.Api.IntegrationTests.Parts;
using FixFlow.Api.IntegrationTests.WorkOrders;

namespace FixFlow.Api.IntegrationTests.ServiceEntries;

public sealed class GetServiceEntryTests(FixFlowApiFactory factory) : ServiceEntryTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Added_Entry_At_Location_When_Dispatcher_Requests_It()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var part = await scenario.DispatcherClient.CreatePartAsync(PartRequests.NewPart());
        var request = ServiceEntryRequests.WorkEntry(scenario.WorkOrder, new ServiceEntryPartRequest(part.Id, 2));
        using var addResponse = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request);
        var addedEntry = (await addResponse.Content.ReadFromJsonAsync<ServiceEntryResponse>(ApiJson.Options, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        var entry = await scenario.DispatcherClient.GetFromJsonAsync<ServiceEntryResponse>(addResponse.Headers.Location, ApiJson.Options, TestContext.Current.CancellationToken);

        addResponse.Headers.Location.ShouldBe(ServiceEntryRequests.ServiceEntryUri(scenario.WorkOrder.Id, addedEntry.Id));
        entry.ShouldNotBeNull();
        entry.Id.ShouldBe(addedEntry.Id);
        entry.Note.ShouldBe(request.Note);
        entry.TechnicianName.ShouldBe("Jan Kowalski");
        var usedPart = entry.Parts.ShouldHaveSingleItem();
        usedPart.PartName.ShouldBe(part.Name);
        usedPart.Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Return_Entry_When_Assigned_Technician_Requests_It()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var addedEntry = await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.WorkEntry(scenario.WorkOrder));

        var entry = await scenario.TechnicianClient.GetServiceEntryAsync(scenario.WorkOrder.Id, addedEntry.Id);

        entry.Id.ShouldBe(addedEntry.Id);
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Entry_Belongs_To_Another_Work_Order()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var addedEntry = await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.WorkEntry(scenario.WorkOrder));
        var otherWorkOrder = await scenario.DispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(scenario.WorkOrder.DeviceId));

        using var response = await scenario.DispatcherClient.GetAsync(
            ServiceEntryRequests.ServiceEntryUri(otherWorkOrder.Id, addedEntry.Id),
            TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, ServiceEntryErrors.NotFound.Code);
    }
}
