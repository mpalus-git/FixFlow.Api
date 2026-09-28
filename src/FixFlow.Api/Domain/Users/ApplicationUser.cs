using Microsoft.AspNetCore.Identity;

namespace FixFlow.Api.Domain.Users;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset? DeactivatedAt { get; private set; }

    public bool IsActive => DeactivatedAt is null;

    public void Deactivate(DateTimeOffset now)
    {
        DeactivatedAt ??= now;
    }

    public void Activate()
    {
        DeactivatedAt = null;
    }
}
