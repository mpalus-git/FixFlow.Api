using FixFlow.Api.Domain.Users;

namespace FixFlow.Api.Common.Auth;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string DispatcherOrAdmin = "DispatcherOrAdmin";
    public const string TechnicianOnly = "TechnicianOnly";

    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(DispatcherOrAdmin, policy => policy.RequireRole(Roles.Dispatcher, Roles.Admin))
            .AddPolicy(TechnicianOnly, policy => policy.RequireRole(Roles.Technician));

        return services;
    }
}
