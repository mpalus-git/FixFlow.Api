using System.ComponentModel;

namespace FixFlow.Api.Features.WorkOrders.StartWork;

[Description("Optional details of starting work on a work order.")]
public sealed record StartWorkRequest(
    [property: Description("Time when the technician started the work, for example recorded offline by the mobile client. Cannot be earlier than the assignment of the work order to the technician and cannot be later than the server time plus the allowed clock skew; a time within the skew is stored as the server time. When omitted, the server time is used.")] DateTimeOffset? StartedAt = null);
