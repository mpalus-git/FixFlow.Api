using FixFlow.Api.Features.Users.CreateUser;
using FixFlow.Api.Features.Users.ListUsers;
using FluentValidation;

namespace FixFlow.Api.Features.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateUserHandler>();
        services.AddSingleton<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<ListUsersHandler>();
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

        return app;
    }
}
