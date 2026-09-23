using System.Text;

namespace FixFlow.Api.Common.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    private const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Issuer)
        && !string.IsNullOrWhiteSpace(Audience)
        && Encoding.UTF8.GetByteCount(SigningKey) >= MinimumSigningKeyBytes
        && AccessTokenLifetime > TimeSpan.Zero
        && RefreshTokenLifetime > AccessTokenLifetime;
}
