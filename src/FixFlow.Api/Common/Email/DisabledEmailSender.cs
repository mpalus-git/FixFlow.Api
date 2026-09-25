namespace FixFlow.Api.Common.Email;

public sealed partial class DisabledEmailSender(ILogger<DisabledEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        LogEmailSkipped(message.Subject, message.Recipients.Count);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sending is disabled; skipped \"{Subject}\" for {RecipientCount} recipients")]
    private partial void LogEmailSkipped(string subject, int recipientCount);
}
