using System.ComponentModel;
using System.Text.Json.Serialization;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;

[Description("New details of an existing work order. All fields are replaced.")]
public sealed record UpdateWorkOrderRequest(
    [property: Description("Description of the fault.")] string Description,
    [property: JsonRequired][property: Description("Priority of the work order.")] WorkOrderPriority Priority,
    [property: Description("UTC deadline of the work order; a changed deadline must be in the future, an unchanged one is accepted even when it has passed.")] DateTimeOffset DueDate);
