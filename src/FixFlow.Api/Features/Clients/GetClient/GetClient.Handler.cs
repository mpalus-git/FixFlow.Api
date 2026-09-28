using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Clients;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Clients.GetClient;

public sealed class GetClientHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<Versioned<ClientResponse>>> HandleAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients.SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);

        return client is null ? ClientErrors.NotFound : dbContext.Versioned(client, ClientResponse.FromDomain(client));
    }
}
