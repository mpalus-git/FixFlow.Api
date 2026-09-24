using System.ComponentModel;
using FixFlow.Api.Common.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Features.Devices.ListDevices;

public sealed record ListDevicesRequest(
    [FromQuery(Name = "page")][property: Description("Page number, starting from 1.")] int Page = 1,
    [FromQuery(Name = "pageSize")][property: Description("Number of devices per page, from 1 to 100.")] int PageSize = PagedRequest.DefaultPageSize,
    [FromQuery(Name = "clientId")][property: Description("Identifier of the client whose devices are listed.")] Guid? ClientId = null,
    [FromQuery(Name = "search")][property: Description("Case-insensitive fragment of the serial number, model or manufacturer.")] string? Search = null) : IPagedRequest;
