using FixFlow.Api.Common.Auth;
using FixFlow.Api.Features.Auth.DeleteExpiredRefreshTokens;
using FixFlow.Api.Features.Auth.Login;
using FixFlow.Api.Features.Auth.Logout;
using FixFlow.Api.Features.Auth.Refresh;
using FluentValidation;

namespace FixFlow.Api.Features.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthFeatures(this IServiceCollection services)
    {
        services.AddSingleton<PasswordVerificationTimingGuard>();
        services.AddScoped<LoginHandler>();
        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<RefreshHandler>();
        services.AddSingleton<IValidator<RefreshRequest>, RefreshRequestValidator>();
        services.AddScoped<LogoutHandler>();
        services.AddSingleton<IValidator<LogoutRequest>, LogoutRequestValidator>();
        services.AddScoped<DeleteExpiredRefreshTokensHandler>();

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
        group.MapLogout();

        return app;
    }
}
