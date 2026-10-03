using System.ComponentModel;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.Dashboard.GetDashboardSummary;

[Description("Aggregated work order counts for the dispatcher dashboard.")]
public sealed record DashboardSummaryResponse(
    [property: Description("UTC time when the summary was calculated.")] DateTimeOffset GeneratedAt,
    [property: Description("Monday of the current week in the Europe/Warsaw time zone.")] DateOnly WeekStart,
    [property: Description("Sunday of the current week in the Europe/Warsaw time zone.")] DateOnly WeekEnd,
    [property: Description("Number of work orders in each status, in lifecycle order, including statuses without work orders.")] IReadOnlyList<WorkOrderStatusCountResponse> StatusCounts,
    [property: Description("Number of work orders flagged as overdue.")] int OverdueCount,
    [property: Description("Workload of every active technician, sorted by full name and email.")] IReadOnlyList<TechnicianWorkloadResponse> Technicians);

[Description("Number of work orders in one status.")]
public sealed record WorkOrderStatusCountResponse(
    [property: Description("Status of the work orders.")] WorkOrderStatus Status,
    [property: Description("Number of work orders in the status.")] int Count);

[Description("Open work orders of one active technician.")]
public sealed record TechnicianWorkloadResponse(
    [property: Description("Identifier of the technician.")] Guid TechnicianId,
    [property: Description("Email of the technician.")] string Email,
    [property: Description("First and last name of the technician.")] string FullName,
    [property: Description("Number of work orders assigned to the technician and not started yet.")] int AssignedCount,
    [property: Description("Number of work orders in progress; at most one.")] int InProgressCount,
    [property: Description("Number of assigned or in-progress work orders flagged as overdue.")] int OverdueCount,
    [property: Description("Number of assigned or in-progress work orders due in the current week.")] int DueThisWeekCount);
