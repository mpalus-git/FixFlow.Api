using System.Text.Json.Serialization;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Cors;
using FixFlow.Api.Common.Email;
using FixFlow.Api.Common.Health;
using FixFlow.Api.Common.Jobs;
using FixFlow.Api.Common.Logging;
using FixFlow.Api.Common.OpenApi;
using FixFlow.Api.Common.Pdf;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Telemetry;
using FixFlow.Api.Features.Auth;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.DemoData;
using FixFlow.Api.Features.Devices;
using FixFlow.Api.Features.Parts;
using FixFlow.Api.Features.ServiceEntries;
using FixFlow.Api.Features.Users;
using FixFlow.Api.Features.WorkOrders;
using Microsoft.AspNetCore.HttpOverrides;

PdfGeneration.Configure();

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardLimit = builder.Configuration.GetValue("ForwardedHeaders:ForwardLimit", 1));
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationCaching(builder.Configuration);
builder.Services.AddSingleton<TimeProvider>(new DatabasePrecisionTimeProvider(TimeProvider.System));
builder.Services.AddApplicationIdentity();
builder.Services.AddJwtAuthentication();
builder.Services.AddAuthRateLimiting();
builder.Services.AddClientCors();
builder.Services.AddEmailSending(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddProblemDetails();
builder.Services.AddApplicationHealthChecks();
builder.Services.AddApiDocumentation();
builder.Services.AddAuthFeatures();
builder.Services.AddUsersFeatures();
builder.Services.AddClientsFeatures();
builder.Services.AddDevicesFeatures();
builder.Services.AddWorkOrdersFeatures();
builder.Services.AddPartsFeatures();
builder.Services.AddServiceEntriesFeatures();
builder.Services.AddDemoDataFeatures();
builder.Services.AddScheduledJobs(builder.Configuration);
builder.Services.AddApplicationTelemetry(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthEndpoints();
app.MapApiDocumentation();
app.MapAuthEndpoints();
app.MapUsersEndpoints();
app.MapClientsEndpoints();
app.MapDevicesEndpoints();
app.MapWorkOrdersEndpoints();
app.MapPartsEndpoints();
app.MapServiceEntriesEndpoints();
app.MapDemoDataEndpoints();

if (!BuildTimeOpenApiGeneration.IsRunning)
{
    await app.InitializeDatabaseAsync();
}

await app.RunAsync();

public partial class Program;
