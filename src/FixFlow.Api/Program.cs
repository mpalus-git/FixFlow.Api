using FixFlow.Api.Common.Logging;
using FixFlow.Api.Common.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services.AddHealthChecks();
builder.Services.AddApiDocumentation();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapHealthChecks("/health");
app.MapApiDocumentation();

app.Run();
