using System.ComponentModel;

namespace FixFlow.Api.Domain.WorkOrders;

[Description("Status of a work order. Allowed transitions: New -> Assigned -> InProgress -> Completed -> Invoiced, and Assigned -> New when the technician is unassigned.")]
public enum WorkOrderStatus
{
    New,
    Assigned,
    InProgress,
    Completed,
    Invoiced,
}
