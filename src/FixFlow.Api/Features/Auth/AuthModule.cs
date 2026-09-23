using FixFlow.Api.Features.Auth.Login;
using FixFlow.Api.Features.Auth.Refresh;
using FluentValidation;

namespace FixFlow.Api.Features.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthFeatures(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<RefreshHandler>();
        services.AddSingleton<IValidator<RefreshRequest>, RefreshRequestValidator>();

        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("Auth")
            .MapGroup("/api/v{version:apiVersion}/auth")
            .HasApiVersion(1)
            .WithTags("Auth");

        group.MapLogin();
        group.MapRefresh();

        return app;
    }
}
