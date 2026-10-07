using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.WorkOrders.CreateWorkOrder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class WorkOrderNumberTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Number_Work_Orders_Consecutively_When_Dispatcher_Creates_Them()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();

        var first = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));
        var second = await client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId));

        var year = BusinessTime.From(first.CreatedAt).Year;
        first.Number.ShouldBe($"ZL/{year}/0001");
        second.Number.ShouldBe($"ZL/{year}/0002");
        (await client.GetWorkOrderAsync(second.Id)).Number.ShouldBe(second.Number);
    }

    [Fact]
    public async Task Should_Start_Numbering_From_One_When_New_Year_Begins_In_Warsaw()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();
        var lastHourOf2025InWarsaw = new DateTimeOffset(2025, 12, 31, 22, 30, 0, TimeSpan.Zero);
        var firstHourOf2026InWarsaw = new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero);

        var lastOf2025 = await CreateWorkOrderAtAsync(deviceId, lastHourOf2025InWarsaw);
        var firstOf2026 = await CreateWorkOrderAtAsync(deviceId, firstHourOf2026InWarsaw);

        lastOf2025.ShouldBe("ZL/2025/0001");
        firstOf2026.ShouldBe("ZL/2026/0001");
    }

    [Fact]
    public async Task Should_Assign_Unique_Consecutive_Numbers_When_Work_Orders_Are_Created_Concurrently()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var deviceId = await client.CreateServicedDeviceAsync();

        var workOrders = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(deviceId))));

        var year = BusinessTime.From(workOrders[0].CreatedAt).Year;
        workOrders.Select(workOrder => workOrder.Number).Order(StringComparer.Ordinal)
            .ShouldBe(Enumerable.Range(1, 5).Select(sequence => $"ZL/{year}/{sequence:D4}"));
    }

    private async Task<string> CreateWorkOrderAtAsync(Guid deviceId, DateTimeOffset now)
    {
        var dispatcher = await CreateUserAsync(Roles.Dispatcher);
        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = new CreateWorkOrderHandler(scope.ServiceProvider.GetRequiredService<FixFlowDbContext>(), new FakeTimeProvider(now));
        var request = WorkOrderRequests.NewWorkOrder(deviceId) with { DueDate = now.AddDays(3) };
        var result = await handler.HandleAsync(request, dispatcher.Id, TestContext.Current.CancellationToken);
        return result.Value.Value.Number;
    }
}
