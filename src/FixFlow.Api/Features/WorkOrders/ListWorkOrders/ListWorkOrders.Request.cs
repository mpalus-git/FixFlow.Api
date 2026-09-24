using System.ComponentModel;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrders;

public sealed record ListWorkOrdersRequest(
    [FromQuery(Name = "page")][property: Description("Page number, starting from 1.")] int Page = 1,
    [FromQuery(Name = "pageSize")][property: Description("Number of work orders per page, from 1 to 100.")] int PageSize = PagedRequest.DefaultPageSize,
    [FromQuery(Name = "status")][property: Description("Status of the listed work orders.")] WorkOrderStatus? Status = null,
    [FromQuery(Name = "technicianId")][property: Description("Identifier of the assigned technician.")] Guid? TechnicianId = null,
    [FromQuery(Name = "deviceId")][property: Description("Identifier of the serviced device.")] Guid? DeviceId = null) : IPagedRequest;
