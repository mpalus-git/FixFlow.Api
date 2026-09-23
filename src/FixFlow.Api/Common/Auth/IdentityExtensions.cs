using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FixFlow.Api.Common.Auth;

public static class IdentityExtensions
{
    public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<FixFlowDbContext>();

        return services;
    }
}
