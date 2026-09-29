using Microsoft.AspNetCore.Identity;

namespace FixFlow.Api.Common.Persistence.Seeding;

public static class IdentityResultExtensions
{
    public static void ThrowIfFailed(this IdentityResult result, string subject)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Seeding '{subject}' failed: {errors}");
        }
    }
}
