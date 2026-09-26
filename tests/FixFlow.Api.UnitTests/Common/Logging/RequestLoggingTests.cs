using System.Net;
using FixFlow.Api.Common.Logging;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog;

namespace FixFlow.Api.UnitTests.Common.Logging;

public sealed class RequestLoggingTests
{
    [Fact]
    public void Should_Log_Client_Ip_When_Request_Has_Remote_Address()
    {
        var diagnosticContext = Substitute.For<IDiagnosticContext>();
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        LoggingExtensions.EnrichWithClientIp(diagnosticContext, httpContext);

        diagnosticContext.Received(1).Set(LoggingExtensions.ClientIpProperty, "203.0.113.10", false);
    }
}
