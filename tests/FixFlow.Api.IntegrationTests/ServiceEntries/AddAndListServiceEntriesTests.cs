using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.Parts;
using FixFlow.Api.Features.ServiceEntries.AddServiceEntry;
using FixFlow.Api.IntegrationTests.Parts;
using FixFlow.Api.IntegrationTests.WorkOrders;

namespace FixFlow.Api.IntegrationTests.ServiceEntries;

public sealed class AddAndListServiceEntriesTests(FixFlowApiFactory factory) : ServiceEntryTestBase(factory)
{
    [Fact]
    public async Task Should_Store_Work_Entry_With_Photos_Time_And_Location_When_Assigned_Technician_Adds_It()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var request = ServiceEntryRequests.WorkEntry(scenario.WorkOrder);

        using var response = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var entry = (await scenario.DispatcherClient.ListServiceEntriesAsync(scenario.WorkOrder.Id)).ShouldHaveSingleItem();
        entry.WorkOrderId.ShouldBe(scenario.WorkOrder.Id);
        entry.TechnicianId.ShouldBe(scenario.WorkOrder.TechnicianId.ShouldNotBeNull());
        entry.IsCorrection.ShouldBeFalse();
        entry.PhotoUrls.ShouldBe(request.PhotoUrls);
        entry.WorkStartedAt.ShouldBe(request.WorkStartedAt);
        entry.Latitude.ShouldBe(request.Latitude);
        entry.Longitude.ShouldBe(request.Longitude);
    }

    [Fact]
    public async Task Should_Return_Parts_To_Stock_And_List_Entries_Oldest_First_When_Correction_Is_Added()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var part = await scenario.DispatcherClient.CreatePartAsync(PartRequests.NewPart() with { StockQuantity = 10, UnitPrice = 40m });
        var workEntry = await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.WorkEntry(scenario.WorkOrder, new ServiceEntryPartRequest(part.Id, 3)));

        var correction = await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.Correction(new ServiceEntryPartRequest(part.Id, 2)));

        correction.IsCorrection.ShouldBeTrue();
        correction.WorkStartedAt.ShouldBeNull();
        correction.Parts.ShouldHaveSingleItem().UnitPrice.ShouldBe(40m);
        (await scenario.DispatcherClient.GetFromJsonAsync<PartResponse>(PartRequests.PartUri(part.Id), TestContext.Current.CancellationToken))
            .ShouldNotBeNull().StockQuantity.ShouldBe(9);
        (await scenario.TechnicianClient.ListServiceEntriesAsync(scenario.WorkOrder.Id)).Select(entry => entry.Id).ShouldBe([workEntry.Id, correction.Id]);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Correction_Returns_More_Than_Was_Used()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var part = await scenario.DispatcherClient.CreatePartAsync(PartRequests.NewPart());
        await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.WorkEntry(scenario.WorkOrder, new ServiceEntryPartRequest(part.Id, 2)));

        using var response = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.Correction(new ServiceEntryPartRequest(part.Id, 3)));

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, ServiceEntryErrors.ReturnExceedsConsumption.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Work_Started_Before_Work_Order_Was_Started()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var request = ServiceEntryRequests.WorkEntry(scenario.WorkOrder) with { WorkStartedAt = scenario.WorkOrder.StartedAt!.Value.AddMinutes(-5) };

        using var response = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request);

        await response.ShouldBeValidationProblemAsync("workStartedAt");
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Work_Entry_Has_No_Work_Time()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var request = ServiceEntryRequests.WorkEntry(scenario.WorkOrder) with { WorkFinishedAt = null };

        using var response = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request);

        await response.ShouldBeValidationProblemAsync("workFinishedAt");
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Used_Part_Does_Not_Exist()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        var request = ServiceEntryRequests.WorkEntry(scenario.WorkOrder, new ServiceEntryPartRequest(Guid.CreateVersion7(), 1));

        using var response = await scenario.TechnicianClient.PostServiceEntryAsync(scenario.WorkOrder.Id, request);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, PartErrors.NotFound.Code);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Work_Order_Is_Not_In_Progress()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician);
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        var workOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await dispatcherClient.CreateServicedDeviceAsync()));
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);
        var request = ServiceEntryRequests.WorkEntry(workOrder) with { WorkStartedAt = DateTimeOffset.UtcNow.AddHours(-1) };

        using var response = await technicianClient.PostServiceEntryAsync(workOrder.Id, request);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.NotInProgress.Code);
    }
}
