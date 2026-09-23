using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Clients;

namespace FixFlow.Api.Features.Clients.CreateClient;

public sealed class CreateClientHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ClientResponse> HandleAsync(CreateClientRequest request, CancellationToken cancellationToken)
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

        return ClientResponse.FromDomain(client);
    }
}
