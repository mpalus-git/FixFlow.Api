using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Telemetry;
using FixFlow.Api.Features.Auth.Login;
using FixFlow.Api.IntegrationTests.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FixFlow.Api.IntegrationTests.Telemetry;

public sealed class TelemetryTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly TimeSpan ActivityWaitTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Should_Trace_Request_With_Route_And_Database_Query_When_Console_Exporter_Is_Enabled()
    {
        using var collector = new CollectingActivityProcessor();
        await using var telemetryFactory = Factory.WithWebHostBuilder(builder => builder
            .UseSetting(TelemetryExtensions.ConsoleExporterEnabledSettingKey, "true")
            .ConfigureServices(services => services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddProcessor(collector))));
        using var client = telemetryFactory.CreateClient();

        using var response = await client.PostAsJsonAsync(AuthRequests.LoginUri, new LoginRequest("nobody@fixflow.test", "Some1!password"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var requestActivity = await collector.WaitForAsync(activity => activity.Source.Name == "Microsoft.AspNetCore");
        requestActivity.GetTagItem("http.route").ShouldBe("/api/v{version:apiVersion}/auth/login");
        await collector.WaitForAsync(activity => activity.Source.Name == "Npgsql" && activity.TraceId == requestActivity.TraceId);
        telemetryFactory.Services.GetService<MeterProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void Should_Not_Register_Telemetry_When_Console_Exporter_Is_Disabled()
    {
        Factory.Services.GetService<TracerProvider>().ShouldBeNull();
        Factory.Services.GetService<MeterProvider>().ShouldBeNull();
    }

    private sealed class CollectingActivityProcessor : BaseProcessor<Activity>
    {
        private readonly ConcurrentQueue<Activity> _activities = new();

        public override void OnEnd(Activity data) => _activities.Enqueue(data);

        public async Task<Activity> WaitForAsync(Func<Activity, bool> predicate)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < ActivityWaitTimeout)
            {
                var activity = _activities.FirstOrDefault(predicate);
                if (activity is not null)
                {
                    return activity;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
            }

            throw new ShouldAssertException($"No matching activity recorded within {ActivityWaitTimeout}.");
        }
    }
}
