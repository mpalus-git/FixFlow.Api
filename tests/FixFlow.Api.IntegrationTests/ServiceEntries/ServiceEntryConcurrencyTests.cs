using System.Net;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.IntegrationTests.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.ServiceEntries;

public sealed class ServiceEntryConcurrencyTests(FixFlowApiFactory factory) : ServiceEntryTestBase(factory)
{
    [Fact]
    public async Task Should_Reject_Service_Entry_When_Work_Order_Was_Completed_After_It_Was_Loaded()
    {
        using var scenario = await CreateWorkOrderInProgressAsync();
        await scenario.TechnicianClient.AddServiceEntryAsync(scenario.WorkOrder.Id, ServiceEntryRequests.WorkEntry(scenario.WorkOrder));
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var workOrderSeenByEntry = await dbContext.WorkOrders.SingleAsync(item => item.Id == scenario.WorkOrder.Id, TestContext.Current.CancellationToken);
        var lateEntry = ServiceEntry.CreateWork(
            workOrderSeenByEntry,
            scenario.WorkOrder.TechnicianId.ShouldNotBeNull(),
            "Late entry",
            [],
            scenario.WorkOrder.StartedAt.ShouldNotBeNull(),
            DateTimeOffset.UtcNow,
            null,
            [],
            DateTimeOffset.UtcNow).Value;

        using var completeResponse = await scenario.TechnicianClient.PostTransitionAsync(scenario.WorkOrder.Id, "complete");
        dbContext.ServiceEntries.Add(lateEntry);
        dbContext.RejectSaveIfChangedConcurrently(workOrderSeenByEntry);
        var saving = await dbContext.SaveChangesOrConflictAsync(TestContext.Current.CancellationToken);

        completeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        saving.FirstError.ShouldBe(SaveChangesConflicts.ConcurrentModification);
        (await scenario.DispatcherClient.ListServiceEntriesAsync(scenario.WorkOrder.Id)).Count.ShouldBe(1);
    }
}
