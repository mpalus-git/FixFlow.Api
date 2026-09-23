using System.ComponentModel;
using FixFlow.Api.Common.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Features.Clients.ListClients;

public sealed record ListClientsRequest(
    [FromQuery(Name = "page")][property: Description("Page number, starting from 1.")] int Page = 1,
    [FromQuery(Name = "pageSize")][property: Description("Number of clients per page, from 1 to 100.")] int PageSize = PagedRequest.DefaultPageSize,
    [FromQuery(Name = "search")][property: Description("Case-insensitive fragment of the client name.")] string? Search = null) : IPagedRequest;
