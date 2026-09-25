using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FixFlow.Api.Common.Email;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private static readonly TimeSpan SmtpTimeout = TimeSpan.FromSeconds(10);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mimeMessage.To.AddRange(message.Recipients.Select(recipient => MailboxAddress.Parse(recipient)));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new TextPart("plain") { Text = message.Body };

        using var smtpClient = new SmtpClient { Timeout = (int)SmtpTimeout.TotalMilliseconds };
        await smtpClient.ConnectAsync(settings.Host, settings.Port, settings.Security, cancellationToken);
        if (settings.HasAuthentication)
        {
            await smtpClient.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        }

        await smtpClient.SendAsync(mimeMessage, cancellationToken);
        await smtpClient.DisconnectAsync(quit: true, cancellationToken);
    }
}
