using System.Security.Claims;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders;

public static class WorkOrderVisibility
{
    public static IQueryable<WorkOrder> VisibleTo(this IQueryable<WorkOrder> workOrders, ClaimsPrincipal user)
    {
        if (!user.IsInRole(Roles.Technician))
        {
            return workOrders;
        }

        var technicianId = user.GetUserId();
        return workOrders.Where(workOrder => workOrder.TechnicianId == technicianId);
    }
}
