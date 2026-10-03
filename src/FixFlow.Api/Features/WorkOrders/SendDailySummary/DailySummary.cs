using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.SendDailySummary;

public sealed record DailySummary(
    DateOnly Day,
    IReadOnlyList<WorkOrderStatusCount> StatusCounts,
    IReadOnlyList<OverdueWorkOrderSummary> OverdueWorkOrders,
    IReadOnlyList<CompletedWorkOrderSummary> CompletedWorkOrders);

public sealed record WorkOrderStatusCount(WorkOrderStatus Status, int Count);

public sealed record OverdueWorkOrderSummary(string Number, string DeviceSerialNumber, WorkOrderPriority Priority, DateTimeOffset DueDate, string? TechnicianName);

public sealed record CompletedWorkOrderSummary(string Number, string DeviceSerialNumber, DateTimeOffset CompletedAt, string? TechnicianName);
