using System.ComponentModel;

namespace FixFlow.Api.Domain.WorkOrders;

[Description("Kind of change recorded in the history of a work order.")]
public enum WorkOrderEventType
{
    Created,
    Updated,
    Assigned,
    Reassigned,
    Unassigned,
    Started,
    Completed,
    Invoiced,
}
