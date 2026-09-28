using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Common.Pagination;

public interface IPagedRequest
{
    int Page { get; }

    int PageSize { get; }
}

public static class PagedRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxPage = 1_000_000;

    public static async Task<PagedResponse<TResult>> ToPagedResponseAsync<TSource, TResult>(
        this IQueryable<TSource> orderedQuery,
        IPagedRequest request,
        Func<TSource, TResult> map,
        CancellationToken cancellationToken)
    {
        var totalCount = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TResult>(items.Select(map).ToList(), request.Page, request.PageSize, totalCount);
    }
}

public sealed class PagedRequestValidator : AbstractValidator<IPagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(request => request.Page).InclusiveBetween(1, PagedRequest.MaxPage);
        RuleFor(request => request.PageSize).InclusiveBetween(1, PagedRequest.MaxPageSize);
    }
}
