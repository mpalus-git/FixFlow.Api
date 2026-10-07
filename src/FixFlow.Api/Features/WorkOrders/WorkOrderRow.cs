using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders;

public sealed class WorkOrderRow
{
    public required WorkOrder WorkOrder { get; init; }

    public required string DeviceSerialNumber { get; init; }

    public required string DeviceModel { get; init; }

    public required Guid ClientId { get; init; }

    public required string ClientName { get; init; }

    public string? TechnicianEmail { get; init; }

    public string? TechnicianName { get; init; }

    public WorkOrderRow WithTechnician(ActiveTechnician? technician) => new()
    {
        WorkOrder = WorkOrder,
        DeviceSerialNumber = DeviceSerialNumber,
        DeviceModel = DeviceModel,
        ClientId = ClientId,
        ClientName = ClientName,
        TechnicianEmail = technician?.Email,
        TechnicianName = technician?.FullName,
    };
}

public static class WorkOrderRows
{
    public static Task<WorkOrderRow?> FindRowAsync(
        this IQueryable<WorkOrder> workOrders,
        Guid workOrderId,
        FixFlowDbContext dbContext,
        CancellationToken cancellationToken) =>
        workOrders
            .Where(workOrder => workOrder.Id == workOrderId)
            .WithRelatedDetails(dbContext)
            .SingleOrDefaultAsync(cancellationToken);

    public static IQueryable<WorkOrderRow> WithRelatedDetails(this IQueryable<WorkOrder> workOrders, FixFlowDbContext dbContext) =>
        from workOrder in workOrders
        join device in dbContext.Devices on workOrder.DeviceId equals device.Id
        join client in dbContext.Clients on device.ClientId equals client.Id
        join technician in dbContext.Users on workOrder.TechnicianId equals technician.Id into technicians
        from technician in technicians.DefaultIfEmpty()
        select new WorkOrderRow
        {
            WorkOrder = workOrder,
            DeviceSerialNumber = device.SerialNumber,
            DeviceModel = device.Model,
            ClientId = client.Id,
            ClientName = client.Name,
            TechnicianEmail = technician == null ? null : technician.Email,
            TechnicianName = technician == null ? null : technician.FullName,
        };
}
