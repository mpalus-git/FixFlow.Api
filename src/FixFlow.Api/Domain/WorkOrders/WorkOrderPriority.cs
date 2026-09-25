using System.ComponentModel;

namespace FixFlow.Api.Domain.WorkOrders;

[Description("Priority of a work order.")]
public enum WorkOrderPriority
{
    Low,
    Normal,
    High,
    Critical,
}
