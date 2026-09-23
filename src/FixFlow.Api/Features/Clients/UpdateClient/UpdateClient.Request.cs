using System.ComponentModel;

namespace FixFlow.Api.Features.Clients.UpdateClient;

[Description("New data of an existing client. All fields are replaced.")]
public sealed record UpdateClientRequest(
    [property: Description("Company or person name.")] string Name,
    [property: Description("Postal address.")] ClientAddress Address,
    [property: Description("Contact person at the client.")] string ContactPerson,
    [property: Description("Contact phone number.")] string Phone,
    [property: Description("Optional contact email address.")] string? Email = null) : IClientDetails;
