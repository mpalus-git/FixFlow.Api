using System.ComponentModel;
using FixFlow.Api.Domain.Clients;

namespace FixFlow.Api.Features.Clients;

[Description("Client of the service company.")]
public sealed record ClientResponse(
    [property: Description("Identifier of the client.")] Guid Id,
    [property: Description("Company or person name.")] string Name,
    [property: Description("Postal address.")] ClientAddress Address,
    [property: Description("Contact person at the client.")] string ContactPerson,
    [property: Description("Contact phone number.")] string Phone,
    [property: Description("Contact email address.")] string? Email,
    [property: Description("UTC time when the client was created.")] DateTimeOffset CreatedAt,
    [property: Description("UTC time when the client was archived; null for active clients.")] DateTimeOffset? ArchivedAt)
{
    public static ClientResponse FromDomain(Client client) => new(
        client.Id,
        client.Name,
        ClientAddress.FromDomain(client.Address),
        client.ContactPerson,
        client.Phone,
        client.Email,
        client.CreatedAt,
        client.ArchivedAt);
}
