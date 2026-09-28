using System.Net;
using FixFlow.Api.Common.Logging;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog;
using Serilog.Events;

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

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public void Should_Log_At_Verbose_Level_When_Health_Check_Succeeds(string path)
    {
        var level = LoggingExtensions.GetRequestLogLevel(CreateHttpContext(path, StatusCodes.Status200OK), 1, null);

        level.ShouldBe(LogEventLevel.Verbose);
    }

    [Fact]
    public void Should_Log_At_Error_Level_When_Health_Check_Reports_Unhealthy()
    {
        var level = LoggingExtensions.GetRequestLogLevel(CreateHttpContext("/health/ready", StatusCodes.Status503ServiceUnavailable), 1, null);

        level.ShouldBe(LogEventLevel.Error);
    }

    [Theory]
    [InlineData("/api/v1/clients", StatusCodes.Status200OK)]
    [InlineData("/healthcheck", StatusCodes.Status404NotFound)]
    public void Should_Log_At_Information_Level_When_Request_Is_Not_Health_Check(string path, int statusCode)
    {
        var level = LoggingExtensions.GetRequestLogLevel(CreateHttpContext(path, statusCode), 1, null);

        level.ShouldBe(LogEventLevel.Information);
    }

    [Fact]
    public void Should_Log_At_Error_Level_When_Request_Throws()
    {
        var level = LoggingExtensions.GetRequestLogLevel(CreateHttpContext("/health", StatusCodes.Status200OK), 1, new InvalidOperationException());

        level.ShouldBe(LogEventLevel.Error);
    }

    private static DefaultHttpContext CreateHttpContext(string path, int statusCode)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.Response.StatusCode = statusCode;
        return httpContext;
    }
}
