using System.Globalization;
using System.Text;
using FixFlow.Api.Common.Email;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.SendDailySummary;

public static class DailySummaryEmail
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm";

    public static EmailMessage Create(DailySummary summary, IReadOnlyList<string> recipients)
    {
        var day = summary.Day.ToString(DateFormat, CultureInfo.InvariantCulture);
        var body = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"FixFlow daily summary for {day}")
            .AppendLine()
            .AppendLine("Work orders by status");

        foreach (var statusCount in summary.StatusCounts)
        {
            body.AppendLine(CultureInfo.InvariantCulture, $"- {DescribeStatus(statusCount.Status)}: {statusCount.Count}");
        }

        body.AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Overdue work orders ({summary.OverdueWorkOrders.Count})");
        if (summary.OverdueWorkOrders.Count == 0)
        {
            body.AppendLine("No overdue work orders.");
        }

        foreach (var workOrder in summary.OverdueWorkOrders)
        {
            body.AppendLine(CultureInfo.InvariantCulture, $"- {workOrder.DeviceSerialNumber} | {workOrder.Priority} | due {FormatBusinessTime(workOrder.DueDate)} | {DescribeTechnician(workOrder.TechnicianName)}");
        }

        body.AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Completed on {day} ({summary.CompletedWorkOrders.Count})");
        if (summary.CompletedWorkOrders.Count == 0)
        {
            body.AppendLine("No work orders were completed.");
        }

        foreach (var workOrder in summary.CompletedWorkOrders)
        {
            body.AppendLine(CultureInfo.InvariantCulture, $"- {workOrder.DeviceSerialNumber} | completed {FormatBusinessTime(workOrder.CompletedAt)} | {DescribeTechnician(workOrder.TechnicianName)}");
        }

        return new EmailMessage(recipients, $"FixFlow daily summary for {day}", body.ToString());
    }

    private static string DescribeStatus(WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.InProgress => "In progress",
        WorkOrderStatus.Completed => "Completed, awaiting invoice",
        _ => status.ToString(),
    };

    private static string DescribeTechnician(string? technicianName) =>
        technicianName is null ? "no technician" : $"technician {technicianName}";

    private static string FormatBusinessTime(DateTimeOffset moment) =>
        BusinessTime.From(moment).ToString(DateTimeFormat, CultureInfo.InvariantCulture);
}
