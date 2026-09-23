using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Cors;
using FixFlow.Api.Common.Health;
using FixFlow.Api.Common.Logging;
using FixFlow.Api.Common.OpenApi;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Features.Auth;
using FixFlow.Api.Features.Users;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplicationIdentity();
builder.Services.AddJwtAuthentication();
builder.Services.AddAuthRateLimiting();
builder.Services.AddClientCors();
builder.Services.AddProblemDetails();
builder.Services.AddApplicationHealthChecks();
builder.Services.AddApiDocumentation();
builder.Services.AddAuthFeatures();
builder.Services.AddUsersFeatures();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthEndpoints();
app.MapApiDocumentation();
app.MapAuthEndpoints();
app.MapUsersEndpoints();

if (!BuildTimeOpenApiGeneration.IsRunning)
{
    await app.InitializeDatabaseAsync();
}

await app.RunAsync();

public partial class Program;
