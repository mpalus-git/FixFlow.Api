using System.ComponentModel;
using FixFlow.Api.Common.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Features.Parts.ListParts;

public sealed record ListPartsRequest(
    [FromQuery(Name = "page")][property: Description("Page number, starting from 1.")] int Page = 1,
    [FromQuery(Name = "pageSize")][property: Description("Number of parts per page, from 1 to 100.")] int PageSize = PagedRequest.DefaultPageSize,
    [FromQuery(Name = "search")][property: Description("Case-insensitive fragment of the name or catalog number.")] string? Search = null) : IPagedRequest;
