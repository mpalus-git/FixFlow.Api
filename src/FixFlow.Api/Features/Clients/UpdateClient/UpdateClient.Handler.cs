using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Clients;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Clients.UpdateClient;

public sealed class UpdateClientHandler(FixFlowDbContext dbContext)
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

        return ClientResponse.FromDomain(client);
    }
}
