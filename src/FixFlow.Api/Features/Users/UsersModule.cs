using FixFlow.Api.Features.Users.ActivateUser;
using FixFlow.Api.Features.Users.ChangePassword;
using FixFlow.Api.Features.Users.CreateUser;
using FixFlow.Api.Features.Users.DeactivateUser;
using FixFlow.Api.Features.Users.GetCurrentUser;
using FixFlow.Api.Features.Users.ListUsers;
using FixFlow.Api.Features.Users.ResetPassword;
using FixFlow.Api.Features.Users.UpdateUser;
using FluentValidation;

namespace FixFlow.Api.Features.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateUserHandler>();
        services.AddSingleton<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<ListUsersHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<DeactivateUserHandler>();
        services.AddScoped<ActivateUserHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddSingleton<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
        services.AddSingleton<IValidator<ResetPasswordRequest>, ResetPasswordRequestValidator>();
        services.AddSingleton<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();
        services.AddSingleton<IValidator<ListUsersRequest>, ListUsersRequestValidator>();

        return services;
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("Users")
            .MapGroup("/api/v{version:apiVersion}/users")
            .HasApiVersion(1)
            .WithTags("Users");

        group.MapCreateUser();
        group.MapListUsers();
        group.MapGetCurrentUser();
        group.MapChangePassword();
        group.MapDeactivateUser();
        group.MapActivateUser();
        group.MapResetPassword();
        group.MapUpdateUser();

        return app;
    }
}
