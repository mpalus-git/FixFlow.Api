using FixFlow.Api.Domain.Users;

namespace FixFlow.Api.Common.Auth;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";

    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AdminOnly, policy => policy.RequireRole(Roles.Admin));

        return services;
    }
}
