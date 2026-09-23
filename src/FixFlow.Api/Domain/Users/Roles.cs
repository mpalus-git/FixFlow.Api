namespace FixFlow.Api.Domain.Users;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Dispatcher = "Dispatcher";
    public const string Technician = "Technician";

    public static IReadOnlyList<string> All { get; } = [Admin, Dispatcher, Technician];
}
