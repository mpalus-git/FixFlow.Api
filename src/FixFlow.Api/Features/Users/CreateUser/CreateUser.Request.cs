using System.ComponentModel;

namespace FixFlow.Api.Features.Users.CreateUser;

[Description("Data of a new user account created by an administrator.")]
public sealed record CreateUserRequest(
    [property: Description("Email address used as the login of the new account.")] string Email,
    [property: Description("First and last name of the user, up to 100 characters.")] string FullName,
    [property: Description("Initial password. At least 8 characters with an uppercase letter, a lowercase letter, a digit and a non-alphanumeric character.")] string Password,
    [property: Description("Role of the new account: Admin, Dispatcher or Technician.")] string Role);
