namespace FixFlow.Api.Common.Persistence;

public static class LikePattern
{
    public const string EscapeCharacter = @"\";

    public static string Contains(string value) =>
        $"%{value.Replace(@"\", @"\\", StringComparison.Ordinal).Replace("%", @"\%", StringComparison.Ordinal).Replace("_", @"\_", StringComparison.Ordinal)}%";
}
