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
    [FromQuery(Name = "deviceId")][property: Description("Identifier of the serviced device.")] Guid? DeviceId = null,
    [FromQuery(Name = "clientId")][property: Description("Identifier of the client whose devices were serviced.")] Guid? ClientId = null,
    [FromQuery(Name = "isOverdue")][property: Description("When true, lists only overdue work orders; when false, only work orders that are not overdue.")] bool? IsOverdue = null,
    [FromQuery(Name = "dueFrom")][property: Description("First day of the due date range, inclusive, as a calendar date in the Europe/Warsaw time zone.")] DateOnly? DueFrom = null,
    [FromQuery(Name = "dueTo")][property: Description("Last day of the due date range, inclusive, as a calendar date in the Europe/Warsaw time zone.")] DateOnly? DueTo = null,
    [FromQuery(Name = "search")][property: Description("Case-insensitive fragment of the fault description, the device serial number or the client name.")] string? Search = null) : IPagedRequest;
