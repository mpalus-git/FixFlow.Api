using ErrorOr;
using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Features.Clients.UpdateClient;

public sealed class UpdateClientHandler(FixFlowDbContext dbContext, HybridCache cache)
{
    public async Task<ErrorOr<ClientResponse>> HandleAsync(Guid clientId, UpdateClientRequest request, CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients.SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);
        if (client is null)
        {
            return ClientErrors.NotFound;
        }

        var update = client.Update(
            request.Name,
            request.Address.ToDomain(),
            request.ContactPerson,
            request.Phone,
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email);
        if (update.IsError)
        {
            return update.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(CacheTags.Clients, cancellationToken);

        return ClientResponse.FromDomain(client);
    }
}
