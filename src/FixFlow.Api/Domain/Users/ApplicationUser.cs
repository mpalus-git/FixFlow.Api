using Microsoft.AspNetCore.Identity;

namespace FixFlow.Api.Domain.Users;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public const int FullNameMaxLength = 100;

    public string FullName { get; private set; } = string.Empty;

    public DateTimeOffset? DeactivatedAt { get; private set; }

    public bool IsActive => DeactivatedAt is null;

    public void Deactivate(DateTimeOffset now)
    {
        DeactivatedAt ??= now;
    }

    public void ChangeFullName(string fullName)
    {
        FullName = fullName.Trim();
    }

    public void Activate()
    {
        DeactivatedAt = null;
    }
}
