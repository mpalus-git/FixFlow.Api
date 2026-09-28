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
        using var mimeMessage = CreateMimeMessage(message, settings);

        using var smtpClient = new SmtpClient { Timeout = (int)SmtpTimeout.TotalMilliseconds };
        await smtpClient.ConnectAsync(settings.Host, settings.Port, settings.Security, cancellationToken);
        if (settings.HasAuthentication)
        {
            await smtpClient.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        }

        await smtpClient.SendAsync(mimeMessage, cancellationToken);
        await smtpClient.DisconnectAsync(quit: true, cancellationToken);
    }

    public static MimeMessage CreateMimeMessage(EmailMessage message, EmailOptions settings)
    {
        var sender = new MailboxAddress(settings.FromName, settings.FromAddress);
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(sender);
        mimeMessage.To.Add(sender);
        mimeMessage.Bcc.AddRange(message.Recipients.Select(recipient => MailboxAddress.Parse(recipient)));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new TextPart("plain") { Text = message.Body };
        return mimeMessage;
    }
}
