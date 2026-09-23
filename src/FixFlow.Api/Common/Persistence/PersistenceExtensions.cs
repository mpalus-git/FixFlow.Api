using FixFlow.Api.Common.OpenApi;
using FixFlow.Api.Common.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Common.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FixFlowDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention());

        services.AddOptions<DemoUsersOptions>()
            .BindConfiguration(DemoUsersOptions.SectionName)
            .Validate(
                options => !options.Enabled || options.HasAllPasswords(),
                "Demo user passwords are required when demo users seeding is enabled.")
            .ValidateOnStartOutsideBuildTimeGeneration();
        services.AddScoped<IdentitySeeder>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        await dbContext.Database.MigrateAsync();

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await identitySeeder.SeedAsync();
    }
}
