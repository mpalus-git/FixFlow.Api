namespace FixFlow.Api.Common.Persistence.Seeding;

public sealed class DemoUsersOptions
{
    public const string SectionName = "Seed:DemoUsers";

    public const string AdminEmail = "admin@fixflow.local";
    public const string DispatcherEmail = "dispatcher@fixflow.local";
    public const string TechnicianEmail = "technician@fixflow.local";
    public const string AdminFullName = "Agnieszka Wiśniewska";
    public const string DispatcherFullName = "Katarzyna Nowak";
    public const string TechnicianFullName = "Jan Kowalski";

    public bool Enabled { get; init; }

    public string AdminPassword { get; init; } = string.Empty;

    public string DispatcherPassword { get; init; } = string.Empty;

    public string TechnicianPassword { get; init; } = string.Empty;

    public bool HasAllPasswords() =>
        !string.IsNullOrWhiteSpace(AdminPassword)
        && !string.IsNullOrWhiteSpace(DispatcherPassword)
        && !string.IsNullOrWhiteSpace(TechnicianPassword);
}
