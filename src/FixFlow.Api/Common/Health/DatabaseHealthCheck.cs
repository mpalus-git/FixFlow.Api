using FixFlow.Api.Common.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FixFlow.Api.Common.Health;

public sealed class DatabaseHealthCheck(FixFlowDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : new HealthCheckResult(context.Registration.FailureStatus);
}
