using Quartz;

namespace FixFlow.Api.Features.WorkOrders.MarkOverdueWorkOrders;

[DisallowConcurrentExecution]
public sealed class MarkOverdueWorkOrdersJob(MarkOverdueWorkOrdersHandler handler) : IJob
{
    public const string HourlyCronExpression = "0 0 * * * ?";

    public static readonly JobKey Key = new(nameof(MarkOverdueWorkOrdersJob));

    public static void Schedule(IQuartzBuilder quartz)
    {
        quartz.AddJob<MarkOverdueWorkOrdersJob>(job => job.WithIdentity(Key));
        quartz.AddTrigger(trigger => trigger
            .ForJob(Key)
            .WithIdentity($"{Key.Name}.OnStartup")
            .StartNow());
        quartz.AddTrigger(trigger => trigger
            .ForJob(Key)
            .WithIdentity($"{Key.Name}.Hourly")
            .WithCronSchedule(HourlyCronExpression, cron => cron.InTimeZone(TimeZoneInfo.Utc)));
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) =>
        await handler.HandleAsync(cancellationToken);
}
