using System.Globalization;

namespace FixFlow.Api.Common.Concurrency;

public static class EntityTag
{
    public const string VersionProperty = "Version";

    private const string AnyTag = "*";

    public static string Format(uint version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    public static bool Matches(string ifMatch, uint version)
    {
        var currentTag = Format(version);
        return ifMatch
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(tag => tag == AnyTag || tag == currentTag);
    }
}
