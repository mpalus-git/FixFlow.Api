using FixFlow.Api.Domain.Users;

namespace FixFlow.Api.Common.Auth;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string DispatcherOrAdmin = "DispatcherOrAdmin";

    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(DispatcherOrAdmin, policy => policy.RequireRole(Roles.Dispatcher, Roles.Admin));

        return services;
    }
}
