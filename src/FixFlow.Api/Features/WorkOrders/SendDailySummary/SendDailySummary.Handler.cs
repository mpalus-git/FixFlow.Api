using FixFlow.Api.Common.Email;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.SendDailySummary;

public sealed partial class SendDailySummaryHandler(
    FixFlowDbContext dbContext,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<SendDailySummaryHandler> logger)
{
    private static readonly WorkOrderStatus[] ReportedStatuses =
        [WorkOrderStatus.New, WorkOrderStatus.Assigned, WorkOrderStatus.InProgress, WorkOrderStatus.Completed];

    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var period = SummaryPeriod.PreviousDay(now);
        var recipients = await GetRecipientsAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            LogNoRecipients(period.Day);
            return;
        }

        var summary = new DailySummary(
            period.Day,
            await CountWorkOrdersByStatusAsync(cancellationToken),
            await GetOverdueWorkOrdersAsync(now, cancellationToken),
            await GetCompletedWorkOrdersAsync(period, cancellationToken));

        await emailSender.SendAsync(DailySummaryEmail.Create(summary, recipients), cancellationToken);
        LogSummarySent(period.Day, recipients.Count);
    }

    private async Task<List<string>> GetRecipientsAsync(CancellationToken cancellationToken)
    {
        var emails = await dbContext.UsersWithRoles()
            .Where(candidate => (candidate.RoleName == Roles.Dispatcher || candidate.RoleName == Roles.Admin) && candidate.User.Email != null)
            .Select(candidate => candidate.User.Email)
            .Distinct()
            .OrderBy(email => email)
            .ToListAsync(cancellationToken);

        return emails.ConvertAll(email => email!);
    }

    private async Task<List<WorkOrderStatusCount>> CountWorkOrdersByStatusAsync(CancellationToken cancellationToken)
    {
        var counts = await dbContext.WorkOrders.CountByStatusAsync(cancellationToken);

        return [.. ReportedStatuses.Select(status => new WorkOrderStatusCount(status, counts.GetValueOrDefault(status)))];
    }

    private Task<List<OverdueWorkOrderSummary>> GetOverdueWorkOrdersAsync(DateTimeOffset now, CancellationToken cancellationToken) => (
        from workOrder in dbContext.WorkOrders.AsNoTracking().Where(WorkOrder.IsPastDueAt(now))
        join device in dbContext.Devices on workOrder.DeviceId equals device.Id
        join user in dbContext.Users on workOrder.TechnicianId equals (Guid?)user.Id into technicians
        from technician in technicians.DefaultIfEmpty()
        orderby workOrder.DueDate, workOrder.Id
        select new OverdueWorkOrderSummary(workOrder.Number, device.SerialNumber, workOrder.Priority, workOrder.DueDate, technician.FullName))
        .ToListAsync(cancellationToken);

    private Task<List<CompletedWorkOrderSummary>> GetCompletedWorkOrdersAsync(SummaryPeriod period, CancellationToken cancellationToken) => (
        from workOrder in dbContext.WorkOrders.AsNoTracking()
        where workOrder.CompletedAt >= period.Start && workOrder.CompletedAt < period.End
        join device in dbContext.Devices on workOrder.DeviceId equals device.Id
        join user in dbContext.Users on workOrder.TechnicianId equals (Guid?)user.Id into technicians
        from technician in technicians.DefaultIfEmpty()
        orderby workOrder.CompletedAt, workOrder.Id
        select new CompletedWorkOrderSummary(workOrder.Number, device.SerialNumber, workOrder.CompletedAt!.Value, technician.FullName))
        .ToListAsync(cancellationToken);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent daily summary for {Day} to {RecipientCount} recipients")]
    private partial void LogSummarySent(DateOnly day, int recipientCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Daily summary for {Day} was not sent because there are no dispatchers or admins with an email address")]
    private partial void LogNoRecipients(DateOnly day);
}
