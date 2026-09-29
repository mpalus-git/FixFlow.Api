using FixFlow.Api.Common.OpenApi;
using FixFlow.Api.Common.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FixFlow.Api.Common.Persistence;

public static class PersistenceExtensions
{
    private const int MaxRetryCount = 3;

    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Database"))
        {
            GssEncryptionMode = GssEncryptionMode.Disable,
        }.ConnectionString;

        services.AddDbContext<FixFlowDbContext>(options => options
            .UseNpgsql(
                connectionString,
                npgsql => npgsql
                    .MigrationsHistoryTable("__ef_migrations_history")
                    .EnableRetryOnFailure(MaxRetryCount, MaxRetryDelay, errorCodesToAdd: null))
            .UseSnakeCaseNamingConvention());

        services.AddOptions<DemoUsersOptions>()
            .BindConfiguration(DemoUsersOptions.SectionName)
            .Validate(
                options => !options.Enabled || options.HasAllPasswords(),
                "Demo user passwords are required when demo users seeding is enabled.")
            .ValidateOnStartOutsideBuildTimeGeneration();
        services.AddScoped<IdentitySeeder>();
        services.AddOptions<DemoDataOptions>()
            .BindConfiguration(DemoDataOptions.SectionName)
            .Validate<IOptions<DemoUsersOptions>>(
                (options, demoUsersOptions) => !options.Enabled || demoUsersOptions.Value.Enabled,
                "Demo users seeding must be enabled when demo data seeding is enabled.")
            .ValidateOnStartOutsideBuildTimeGeneration();
        services.AddScoped<DemoDataSeeder>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        await dbContext.Database.MigrateAsync();

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await identitySeeder.SeedAsync();

        if (scope.ServiceProvider.GetRequiredService<IOptions<DemoDataOptions>>().Value.Enabled)
        {
            var demoDataSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            await demoDataSeeder.SeedIfDatabaseIsEmptyAsync(app.Lifetime.ApplicationStopping);
        }
    }
}
