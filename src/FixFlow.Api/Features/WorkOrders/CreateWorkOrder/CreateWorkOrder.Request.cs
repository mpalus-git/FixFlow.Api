using System.ComponentModel;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.CreateWorkOrder;

[Description("Data of a new work order.")]
public sealed record CreateWorkOrderRequest(
    [property: Description("Identifier of an active device to service.")] Guid DeviceId,
    [property: Description("Description of the fault.")] string Description,
    [property: Description("UTC deadline of the work order; must be in the future.")] DateTimeOffset DueDate,
    [property: Description("Priority of the work order; Normal when omitted.")] WorkOrderPriority Priority = WorkOrderPriority.Normal);
