using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.AssignTechnician;

public sealed class AssignTechnicianHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(Guid workOrderId, AssignTechnicianRequest request, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders.SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return WorkOrderErrors.NotFound;
        }

        if (!await IsTechnicianAsync(request.TechnicianId, cancellationToken))
        {
            return WorkOrderErrors.TechnicianNotFound;
        }

        var assignment = workOrder.Assign(request.TechnicianId);
        if (assignment.IsError)
        {
            return assignment.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return dbContext.Versioned(workOrder, WorkOrderResponse.FromDomain(workOrder));
    }

    private Task<bool> IsTechnicianAsync(Guid userId, CancellationToken cancellationToken) =>
        (from userRole in dbContext.UserRoles
         join role in dbContext.Roles on userRole.RoleId equals role.Id
         where userRole.UserId == userId && role.Name == Roles.Technician
         select userRole).AnyAsync(cancellationToken);
}
