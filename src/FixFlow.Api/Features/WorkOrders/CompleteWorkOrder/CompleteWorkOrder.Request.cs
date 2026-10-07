using System.ComponentModel;

namespace FixFlow.Api.Features.WorkOrders.CompleteWorkOrder;

[Description("Optional details of completing a work order.")]
public sealed record CompleteWorkOrderRequest(
    [property: Description("Time when the work order was completed, for example recorded offline by the mobile client. Cannot be earlier than the start of the work order or the end of its last work entry and cannot be later than the server time plus the allowed clock skew; a time within the skew is stored as the server time. When omitted, the server time is used.")] DateTimeOffset? CompletedAt = null);
