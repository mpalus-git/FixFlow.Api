using ErrorOr;

namespace FixFlow.Api.Domain.Clients;

public sealed class Client
{
    private Client()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Address Address { get; private set; } = null!;

    public string ContactPerson { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsArchived => ArchivedAt is not null;

    public static Client Create(string name, Address address, string contactPerson, string phone, string? email, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        Address = address,
        ContactPerson = contactPerson,
        Phone = phone,
        Email = email,
        CreatedAt = now,
    };

    public ErrorOr<Updated> Update(string name, Address address, string contactPerson, string phone, string? email)
    {
        if (IsArchived)
        {
            return ClientErrors.Archived;
        }

        Name = name;
        Address = address;
        ContactPerson = contactPerson;
        Phone = phone;
        Email = email;

        return Result.Updated;
    }

    public void Archive(DateTimeOffset now)
    {
        ArchivedAt ??= now;
    }
}
