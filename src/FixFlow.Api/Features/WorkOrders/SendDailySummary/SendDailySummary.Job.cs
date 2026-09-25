using Quartz;

namespace FixFlow.Api.Features.WorkOrders.SendDailySummary;

[DisallowConcurrentExecution]
public sealed class SendDailySummaryJob(SendDailySummaryHandler handler) : IJob
{
    public const string DailyCronExpression = "0 0 7 * * ?";

    public static readonly JobKey Key = new(nameof(SendDailySummaryJob));

    public static void Schedule(IQuartzBuilder quartz)
    {
        quartz.AddJob<SendDailySummaryJob>(job => job.WithIdentity(Key));
        quartz.AddTrigger(trigger => trigger
            .ForJob(Key)
            .WithIdentity($"{Key.Name}.Daily")
            .WithCronSchedule(DailyCronExpression, cron => cron.InTimeZone(SummaryPeriod.BusinessTimeZone)));
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) =>
        await handler.HandleAsync(cancellationToken);
}
