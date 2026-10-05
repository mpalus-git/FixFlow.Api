using System.ComponentModel;
using FixFlow.Api.Common.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Features.Devices.ListDeviceWorkOrders;

public sealed record ListDeviceWorkOrdersRequest(
    [FromQuery(Name = "page")][property: Description("Page number, starting from 1.")] int Page = 1,
    [FromQuery(Name = "pageSize")][property: Description("Number of work orders per page, from 1 to 100.")] int PageSize = PagedRequest.DefaultPageSize) : IPagedRequest;
