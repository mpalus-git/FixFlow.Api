using System.Net.Http.Json;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using FixFlow.Api.Features.WorkOrders.AssignTechnician;
using FixFlow.Api.Features.WorkOrders.CompleteWorkOrder;
using FixFlow.Api.Features.WorkOrders.CreateWorkOrder;
using FixFlow.Api.Features.WorkOrders.ListWorkOrders;
using FixFlow.Api.Features.WorkOrders.StartWork;
using FixFlow.Api.IntegrationTests.Clients;
using FixFlow.Api.IntegrationTests.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public static class WorkOrderRequests
{
    public static readonly Uri WorkOrdersUri = new("/api/v1/work-orders", UriKind.Relative);

    public static Uri WorkOrderUri(Guid workOrderId) => new($"/api/v1/work-orders/{workOrderId}", UriKind.Relative);

    public static CreateWorkOrderRequest NewWorkOrder(Guid deviceId, int dueInDays = 3) =>
        new(deviceId, "Air conditioner is leaking", DateTimeOffset.UtcNow.AddDays(dueInDays), WorkOrderPriority.High);

    public static async Task<Guid> CreateServicedDeviceAsync(this HttpClient client, string serialNumber = "AC-1001")
    {
        var owner = await client.CreateClientAsync(ClientRequests.NewClient());
        var device = await client.CreateDeviceAsync(DeviceRequests.NewDevice(owner.Id, serialNumber));
        return device.Id;
    }

    public static Task<HttpResponseMessage> PostWorkOrderAsync(this HttpClient client, CreateWorkOrderRequest request) =>
        client.PostAsJsonAsync(WorkOrdersUri, request, ApiJson.Options, TestContext.Current.CancellationToken);

    public static async Task<WorkOrderResponse> CreateWorkOrderAsync(this HttpClient client, CreateWorkOrderRequest request)
    {
        using var response = await client.PostWorkOrderAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.ReadWorkOrderAsync();
    }

    public static async Task<WorkOrderResponse> GetWorkOrderAsync(this HttpClient client, Guid workOrderId)
    {
        var workOrder = await client.GetFromJsonAsync<WorkOrderResponse>(WorkOrderUri(workOrderId), ApiJson.Options, TestContext.Current.CancellationToken);
        return workOrder.ShouldNotBeNull();
    }

    public static async Task<PagedResponse<WorkOrderListItemResponse>> ListWorkOrdersAsync(this HttpClient client, string query = "")
    {
        var page = await client.GetFromJsonAsync<PagedResponse<WorkOrderListItemResponse>>(
            new Uri($"/api/v1/work-orders{query}", UriKind.Relative),
            ApiJson.Options,
            TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }

    public static Task<HttpResponseMessage> PostAssignAsync(this HttpClient client, Guid workOrderId, Guid technicianId) =>
        client.PostAsJsonAsync(
            new Uri($"/api/v1/work-orders/{workOrderId}/assign", UriKind.Relative),
            new AssignTechnicianRequest(technicianId),
            TestContext.Current.CancellationToken);

    public static Task<HttpResponseMessage> PostTransitionAsync(this HttpClient client, Guid workOrderId, string transition) =>
        client.PostAsync(new Uri($"/api/v1/work-orders/{workOrderId}/{transition}", UriKind.Relative), null, TestContext.Current.CancellationToken);

    public static Task<HttpResponseMessage> PostCompleteAsync(this HttpClient client, Guid workOrderId, CompleteWorkOrderRequest request) =>
        client.PostAsJsonAsync(
            new Uri($"/api/v1/work-orders/{workOrderId}/complete", UriKind.Relative),
            request,
            ApiJson.Options,
            TestContext.Current.CancellationToken);

    public static Task<HttpResponseMessage> PostStartAsync(this HttpClient client, Guid workOrderId, StartWorkRequest request) =>
        client.PostAsJsonAsync(
            new Uri($"/api/v1/work-orders/{workOrderId}/start", UriKind.Relative),
            request,
            ApiJson.Options,
            TestContext.Current.CancellationToken);

    public static async Task<WorkOrderResponse> ReadWorkOrderAsync(this HttpResponseMessage response)
    {
        var workOrder = await response.Content.ReadFromJsonAsync<WorkOrderResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        return workOrder.ShouldNotBeNull();
    }

    public static async Task AssignTechnicianDirectlyAsync(this FixFlowApiFactory factory, Guid workOrderId, Guid technicianId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var workOrder = await dbContext.WorkOrders.SingleAsync(item => item.Id == workOrderId, TestContext.Current.CancellationToken);
        workOrder.Assign(technicianId, null, DateTimeOffset.UtcNow).IsError.ShouldBeFalse();
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
