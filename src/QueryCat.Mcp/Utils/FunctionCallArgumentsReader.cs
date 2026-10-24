using System.Text.Json;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Mcp.Utils;

internal static class FunctionCallArgumentsReader
{
    /// <summary>
    /// Read function call arguments from JSON. It can be an object with named arguments
    /// or an array with positional arguments.
    /// </summary>
    /// <param name="arguments">Tool arguments.</param>
    /// <param name="key">Arguments key.</param>
    /// <param name="callArguments">Parsed call arguments.</param>
    /// <returns><c>True</c> if arguments were parsed successfully, <c>false</c> otherwise.</returns>
    public static bool TryRead(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string key,
        out FunctionCallArguments callArguments)
    {
        callArguments = new FunctionCallArguments();
        if (!arguments.TryGetValue(key, out var argsElement)
            || argsElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return true;
        }

        // Named arguments.
        if (argsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in argsElement.EnumerateObject())
            {
                if (!VariantValue.TryGetValueFromJsonElement(property.Value, out var value))
                {
                    return false;
                }
                callArguments.Add(property.Name, value);
            }
            return true;
        }

        // Positional arguments.
        if (argsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in argsElement.EnumerateArray())
            {
                if (!VariantValue.TryGetValueFromJsonElement(item, out var value))
                {
                    return false;
                }
                callArguments.Add(value);
            }
            return true;
        }

        return false;
    }
}
