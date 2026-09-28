using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Persistence;

public sealed class MigrationsTests(FixFlowApiFactory factory)
{
    [Fact]
    public async Task Should_Have_No_Pending_Model_Changes_When_Migrations_Are_Applied()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();

        dbContext.Database.HasPendingModelChanges().ShouldBeFalse();
        (await dbContext.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }
}
