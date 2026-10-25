using System.Text.Json;

namespace QueryCat.Mcp.Utils;

internal static class ToolArgumentsUtils
{
    /// <summary>
    /// Get trimmed string argument value.
    /// </summary>
    /// <param name="arguments">Tool arguments.</param>
    /// <param name="key">Argument key.</param>
    /// <returns>Trimmed string value or empty string if the argument is missing or not a string.</returns>
    public static string GetString(IReadOnlyDictionary<string, JsonElement> arguments, string key)
        => arguments.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String
            ? (element.GetString() ?? string.Empty).Trim()
            : string.Empty;

    /// <summary>
    /// Get integer argument value.
    /// </summary>
    /// <param name="arguments">Tool arguments.</param>
    /// <param name="key">Argument key.</param>
    /// <param name="defaultValue">The value to return if the argument is missing or not an integer.</param>
    /// <returns>Integer value.</returns>
    public static int GetInt32(IReadOnlyDictionary<string, JsonElement> arguments, string key, int defaultValue)
        => arguments.TryGetValue(key, out var element)
           && element.ValueKind == JsonValueKind.Number
           && element.TryGetInt32(out var value)
            ? value
            : defaultValue;
}
