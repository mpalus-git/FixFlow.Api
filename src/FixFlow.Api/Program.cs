using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Health;
using FixFlow.Api.Common.Logging;
using FixFlow.Api.Common.OpenApi;
using FixFlow.Api.Common.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationIdentity();
builder.Services.AddProblemDetails();
builder.Services.AddApplicationHealthChecks();
builder.Services.AddApiDocumentation();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
app.MapHealthEndpoints();
app.MapApiDocumentation();

if (!BuildTimeOpenApiGeneration.IsRunning)
{
    await app.InitializeDatabaseAsync();
}

await app.RunAsync();

public partial class Program;
