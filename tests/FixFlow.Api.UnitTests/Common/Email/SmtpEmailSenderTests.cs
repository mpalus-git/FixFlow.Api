using FixFlow.Api.Common.Email;
using MimeKit;

namespace FixFlow.Api.UnitTests.Common.Email;

public sealed class SmtpEmailSenderTests
{
    private static readonly EmailOptions Settings = new() { Enabled = true, Host = "localhost", FromAddress = "noreply@fixflow.local", FromName = "FixFlow" };

    [Fact]
    public void Should_Hide_Recipients_In_Bcc_And_Address_Message_To_Sender_When_Message_Is_Created()
    {
        var message = new EmailMessage(["dispatcher@fixflow.test", "admin@fixflow.test"], "Daily summary", "Body");

        using var mimeMessage = SmtpEmailSender.CreateMimeMessage(message, Settings);

        mimeMessage.To.Mailboxes.Select(mailbox => mailbox.Address).ShouldBe([Settings.FromAddress]);
        mimeMessage.Cc.ShouldBeEmpty();
        mimeMessage.Bcc.Mailboxes.Select(mailbox => mailbox.Address).ShouldBe(message.Recipients);
        mimeMessage.From.Mailboxes.ShouldHaveSingleItem().ShouldBe(new MailboxAddress(Settings.FromName, Settings.FromAddress));
        mimeMessage.Subject.ShouldBe(message.Subject);
        mimeMessage.TextBody.ShouldBe(message.Body);
    }
}
