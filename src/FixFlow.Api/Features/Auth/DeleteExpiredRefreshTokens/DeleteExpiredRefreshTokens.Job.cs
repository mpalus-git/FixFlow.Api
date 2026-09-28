using FixFlow.Api.Common.Time;
using Quartz;

namespace FixFlow.Api.Features.Auth.DeleteExpiredRefreshTokens;

[DisallowConcurrentExecution]
public sealed class DeleteExpiredRefreshTokensJob(DeleteExpiredRefreshTokensHandler handler) : IJob
{
    public const string DailyCronExpression = "0 0 3 * * ?";

    public static readonly JobKey Key = new(nameof(DeleteExpiredRefreshTokensJob));

    public static void Schedule(IQuartzBuilder quartz)
    {
        quartz.AddJob<DeleteExpiredRefreshTokensJob>(job => job.WithIdentity(Key));
        quartz.AddTrigger(trigger => trigger
            .ForJob(Key)
            .WithIdentity($"{Key.Name}.Daily")
            .WithCronSchedule(DailyCronExpression, cron => cron.InTimeZone(BusinessTime.Zone)));
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) =>
        await handler.HandleAsync(cancellationToken);
}
