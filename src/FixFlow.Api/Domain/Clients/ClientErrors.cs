using ErrorOr;

namespace FixFlow.Api.Domain.Clients;

public static class ClientErrors
{
    public static readonly Error NotFound = Error.NotFound("Client.NotFound", "Client was not found.");

    public static readonly Error Archived = Error.Conflict("Client.Archived", "Archived client cannot be modified.");
}
