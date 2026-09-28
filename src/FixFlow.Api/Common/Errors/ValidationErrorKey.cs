using System.Text.Json;

namespace FixFlow.Api.Common.Errors;

public static class ValidationErrorKey
{
    public static string FromPropertyPath(string propertyPath) =>
        string.Join('.', propertyPath.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));

    public static Dictionary<string, string[]> GroupByPropertyPath<TError>(
        IEnumerable<TError> errors,
        Func<TError, string> propertyPath,
        Func<TError, string> message) =>
        errors
            .GroupBy(error => FromPropertyPath(propertyPath(error)))
            .ToDictionary(group => group.Key, group => group.Select(message).ToArray());
}
