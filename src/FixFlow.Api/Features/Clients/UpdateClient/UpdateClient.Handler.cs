using ErrorOr;
using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Features.Clients.UpdateClient;

public sealed class UpdateClientHandler(FixFlowDbContext dbContext, HybridCache cache)
{
    public async Task<ErrorOr<Versioned<ClientResponse>>> HandleAsync(
        Guid clientId,
        UpdateClientRequest request,
        string ifMatch,
        CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients.SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);
        if (client is null)
        {
            return ClientErrors.NotFound;
        }

        var precondition = dbContext.EnsureVersionMatches(client, ifMatch);
        if (precondition.IsError)
        {
            return precondition.Errors;
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

        var saving = await dbContext.SaveChangesOrPreconditionFailedAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        await cache.RemoveByTagAsync([CacheTags.Clients, CacheTags.Devices], cancellationToken);

        return dbContext.Versioned(client, ClientResponse.FromDomain(client));
    }
}
