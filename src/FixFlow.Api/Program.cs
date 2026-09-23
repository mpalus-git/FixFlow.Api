using FixFlow.Api.Common.Logging;
using FixFlow.Api.Common.OpenApi;
using FixFlow.Api.Common.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddApiDocumentation();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapHealthChecks("/health");
app.MapApiDocumentation();

if (!BuildTimeOpenApiGeneration.IsRunning)
{
    await app.MigrateDatabaseAsync();
}

await app.RunAsync();

public partial class Program;
