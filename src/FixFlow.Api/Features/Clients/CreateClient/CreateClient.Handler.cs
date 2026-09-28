using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Clients;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Features.Clients.CreateClient;

public sealed class CreateClientHandler(FixFlowDbContext dbContext, TimeProvider timeProvider, HybridCache cache)
{
    public async Task<Versioned<ClientResponse>> HandleAsync(CreateClientRequest request, CancellationToken cancellationToken)
    {
        var client = Client.Create(
            request.Name,
            request.Address.ToDomain(),
            request.ContactPerson,
            request.Phone,
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            timeProvider.GetUtcNow());

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(CacheTags.Clients, cancellationToken);

        return dbContext.Versioned(client, ClientResponse.FromDomain(client));
    }
}
