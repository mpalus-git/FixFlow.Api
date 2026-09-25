using FixFlow.Api.Common.Email;

namespace FixFlow.Api.UnitTests.Common.Email;

public sealed class EmailOptionsTests
{
    [Fact]
    public void Should_Be_Valid_When_Email_Is_Disabled_Without_Smtp_Settings()
    {
        var options = new EmailOptions { Enabled = false };

        options.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void Should_Be_Valid_When_Enabled_Email_Has_Host_Port_And_Sender()
    {
        var options = new EmailOptions { Enabled = true, Host = "localhost", Port = 1025, FromAddress = "noreply@fixflow.local" };

        options.IsValid().ShouldBeTrue();
    }

    [Theory]
    [InlineData("", 1025, "noreply@fixflow.local")]
    [InlineData("localhost", 0, "noreply@fixflow.local")]
    [InlineData("localhost", 65536, "noreply@fixflow.local")]
    [InlineData("localhost", 1025, " ")]
    public void Should_Be_Invalid_When_Enabled_Email_Misses_Smtp_Settings(string host, int port, string fromAddress)
    {
        var options = new EmailOptions { Enabled = true, Host = host, Port = port, FromAddress = fromAddress };

        options.IsValid().ShouldBeFalse();
    }
}
