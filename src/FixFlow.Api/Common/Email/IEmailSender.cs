namespace FixFlow.Api.Common.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
