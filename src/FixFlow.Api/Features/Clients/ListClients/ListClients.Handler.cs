using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Features.Clients.ListClients;

public sealed class ListClientsHandler(FixFlowDbContext dbContext, HybridCache cache)
{
    public async Task<PagedResponse<ClientResponse>> HandleAsync(ListClientsRequest request, CancellationToken cancellationToken)
    {
        var search = request.Search?.Trim();

        return await cache.GetOrCreateAsync(
            $"clients:list:{request.Page}:{request.PageSize}:{search}",
            async token => await QueryAsync(request, search, token),
            tags: [CacheTags.Clients],
            cancellationToken: cancellationToken);
    }

    private Task<PagedResponse<ClientResponse>> QueryAsync(ListClientsRequest request, string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.Clients
            .AsNoTracking()
            .Where(client => client.ArchivedAt == null);

        if (!string.IsNullOrEmpty(search))
        {
            var pattern = LikePattern.Contains(search);
            query = query.Where(client => EF.Functions.ILike(client.Name, pattern, LikePattern.EscapeCharacter));
        }

        return query
            .OrderBy(client => client.Name)
            .ThenBy(client => client.Id)
            .ToPagedResponseAsync(request, ClientResponse.FromDomain, cancellationToken);
    }
}
