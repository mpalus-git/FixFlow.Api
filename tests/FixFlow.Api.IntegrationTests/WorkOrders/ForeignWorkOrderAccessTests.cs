using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.ServiceEntries.AddServiceEntry;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class ForeignWorkOrderAccessTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Dictionary<string, ForeignWorkOrderRequest> Requests = new()
    {
        ["get work order"] = new(HttpMethod.Get, string.Empty, null),
        ["start work"] = new(HttpMethod.Post, "/start", null),
        ["complete work order"] = new(HttpMethod.Post, "/complete", null),
        ["add service entry"] = new(
            HttpMethod.Post,
            "/service-entries",
            new AddServiceEntryRequest("Replaced filters", WorkStartedAt: DateTimeOffset.UtcNow.AddHours(-2), WorkFinishedAt: DateTimeOffset.UtcNow.AddHours(-1))),
        ["list service entries"] = new(HttpMethod.Get, "/service-entries", null),
        ["get service entry"] = new(HttpMethod.Get, $"/service-entries/{Guid.CreateVersion7()}", null),
        ["get service protocol"] = new(HttpMethod.Get, "/protocol", null),
        ["list work order events"] = new(HttpMethod.Get, "/events", null),
    };

    public static TheoryData<string> RequestNames { get; } = [.. Requests.Keys];

    [Theory]
    [MemberData(nameof(RequestNames))]
    public async Task Should_Return_Not_Found_Problem_When_Technician_Accesses_Work_Order_Of_Another_Technician(string requestName)
    {
        var foreignWorkOrderRequest = Requests[requestName];
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var workOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await dispatcherClient.CreateServicedDeviceAsync()));
        var assignedTechnician = await CreateUserAsync(Roles.Technician);
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, assignedTechnician.Id);
        using var otherTechnicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);
        using var request = new HttpRequestMessage(
            foreignWorkOrderRequest.Method,
            new Uri($"{WorkOrderRequests.WorkOrderUri(workOrder.Id)}{foreignWorkOrderRequest.PathSuffix}", UriKind.Relative));
        if (foreignWorkOrderRequest.Body is not null)
        {
            request.Content = JsonContent.Create(foreignWorkOrderRequest.Body, options: ApiJson.Options);
        }

        using var response = await otherTechnicianClient.SendAsync(request, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, WorkOrderErrors.NotFound.Code);
    }

    private sealed record ForeignWorkOrderRequest(HttpMethod Method, string PathSuffix, AddServiceEntryRequest? Body);
}
