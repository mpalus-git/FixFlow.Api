using System.ComponentModel;

namespace FixFlow.Api.Features.Clients.CreateClient;

[Description("Data of a new client.")]
public sealed record CreateClientRequest(
    [property: Description("Company or person name.")] string Name,
    [property: Description("Postal address.")] ClientAddress Address,
    [property: Description("Contact person at the client.")] string ContactPerson,
    [property: Description("Contact phone number.")] string Phone,
    [property: Description("Optional contact email address.")] string? Email = null) : IClientDetails;
