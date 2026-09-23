using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Clients;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Clients.GetClient;

public sealed class GetClientHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<ClientResponse>> HandleAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients
            .AsNoTracking()
            .SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);

        return client is null ? ClientErrors.NotFound : ClientResponse.FromDomain(client);
    }
}
