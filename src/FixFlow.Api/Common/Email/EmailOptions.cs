using MailKit.Security;

namespace FixFlow.Api.Common.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; init; }

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public SecureSocketOptions Security { get; init; } = SecureSocketOptions.Auto;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = "FixFlow";

    public bool HasAuthentication => !string.IsNullOrWhiteSpace(Username);

    public bool IsValid() =>
        !Enabled
        || (!string.IsNullOrWhiteSpace(Host)
            && Port is > 0 and <= 65535
            && !string.IsNullOrWhiteSpace(FromAddress));
}
