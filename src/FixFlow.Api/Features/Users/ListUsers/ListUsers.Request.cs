using System.ComponentModel;
using FixFlow.Api.Common.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Features.Users.ListUsers;

public sealed record ListUsersRequest(
    [FromQuery(Name = "page")][property: Description("Page number, starting from 1.")] int Page = 1,
    [FromQuery(Name = "pageSize")][property: Description("Number of users per page, from 1 to 100.")] int PageSize = PagedRequest.DefaultPageSize,
    [FromQuery(Name = "role")][property: Description("Role of the listed users: Admin, Dispatcher or Technician.")] string? Role = null) : IPagedRequest;
