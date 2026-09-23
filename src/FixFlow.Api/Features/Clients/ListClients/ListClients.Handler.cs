using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Clients.ListClients;

public sealed class ListClientsHandler(FixFlowDbContext dbContext)
{
    public Task<PagedResponse<ClientResponse>> HandleAsync(ListClientsRequest request, CancellationToken cancellationToken)
    {
        var query = dbContext.Clients
            .AsNoTracking()
            .Where(client => client.ArchivedAt == null);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = LikePattern.Contains(request.Search.Trim());
            query = query.Where(client => EF.Functions.ILike(client.Name, pattern, LikePattern.EscapeCharacter));
        }

        return query
            .OrderBy(client => client.Name)
            .ThenBy(client => client.Id)
            .ToPagedResponseAsync(request, ClientResponse.FromDomain, cancellationToken);
    }
}
