using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace FixFlow.Api.UnitTests.Common.Persistence;

public sealed class PersistenceExtensionsTests
{
    [Fact]
    public void Should_Retry_Transient_Database_Failures_When_Persistence_Is_Registered()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = "Host=localhost;Database=fixflow",
            })
            .Build();
        using var serviceProvider = new ServiceCollection().AddPersistence(configuration).BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();

        var strategy = dbContext.Database.CreateExecutionStrategy();

        strategy.ShouldBeOfType<NpgsqlRetryingExecutionStrategy>().RetriesOnFailure.ShouldBeTrue();
    }
}
