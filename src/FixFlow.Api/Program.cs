using FixFlow.Api.Common.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddApiDocumentation();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapApiDocumentation();

app.Run();
