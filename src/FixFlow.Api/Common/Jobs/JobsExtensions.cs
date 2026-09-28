using FixFlow.Api.Common.OpenApi;
using FixFlow.Api.Features.Auth.DeleteExpiredRefreshTokens;
using FixFlow.Api.Features.WorkOrders.MarkOverdueWorkOrders;
using FixFlow.Api.Features.WorkOrders.SendDailySummary;
using Quartz;

namespace FixFlow.Api.Common.Jobs;

public static class JobsExtensions
{
    public const string EnabledSettingKey = "Jobs:Enabled";

    public static IServiceCollection AddScheduledJobs(this IServiceCollection services, IConfiguration configuration)
    {
        if (BuildTimeOpenApiGeneration.IsRunning || !configuration.GetValue<bool>(EnabledSettingKey))
        {
            return services;
        }

        services.AddQuartz(quartz =>
        {
            MarkOverdueWorkOrdersJob.Schedule(quartz);
            SendDailySummaryJob.Schedule(quartz);
            DeleteExpiredRefreshTokensJob.Schedule(quartz);
        });
        services.AddQuartzHostedService(options =>
        {
            options.AwaitApplicationStarted = true;
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}
