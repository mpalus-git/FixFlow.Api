using System.ComponentModel;

namespace FixFlow.Api.Common.Pagination;

[Description("One page of a list.")]
public sealed record PagedResponse<T>(
    [property: Description("Items on the requested page.")] IReadOnlyList<T> Items,
    [property: Description("Requested page number, starting from 1.")] int Page,
    [property: Description("Requested page size.")] int PageSize,
    [property: Description("Number of items on all pages.")] int TotalCount);
