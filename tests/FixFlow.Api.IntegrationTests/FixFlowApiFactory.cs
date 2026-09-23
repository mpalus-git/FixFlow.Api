using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(FixFlow.Api.IntegrationTests.FixFlowApiFactory))]
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace FixFlow.Api.IntegrationTests;

public sealed class FixFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private Respawner? _respawner;

    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public string DemoUsersPassword { get; } = $"Fx1!{Guid.NewGuid():N}";

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        using var client = CreateClient();

        await using var connection = await OpenDatabaseConnectionAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Respawn.Graph.Table("__ef_migrations_history"), new Respawn.Graph.Table("roles")],
        });
    }

    public async Task ResetDatabaseAsync()
    {
        ArgumentNullException.ThrowIfNull(_respawner);
        await using var connection = await OpenDatabaseConnectionAsync();
        await _respawner.ResetAsync(connection);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "10000");
        builder.UseSetting("Seed:DemoUsers:Enabled", "true");
        builder.UseSetting("Seed:DemoUsers:AdminPassword", DemoUsersPassword);
        builder.UseSetting("Seed:DemoUsers:DispatcherPassword", DemoUsersPassword);
        builder.UseSetting("Seed:DemoUsers:TechnicianPassword", DemoUsersPassword);
    }

    private async Task<NpgsqlConnection> OpenDatabaseConnectionAsync()
    {
        var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }
}
