using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Path;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Functions;

/// <summary>
/// JSON functions.
/// </summary>
public static class JsonFunctions
{
    [SafeFunction]
    [Description("Extracts an object or an array from a JSON string.")]
    [FunctionSignature("json_query(json: string, query: string): string")]
    public static VariantValue JsonQuery(FunctionCallInfo args)
    {
        // Parse input.
        var json = args.GetAt(0).AsString;
        var query = args.GetAt(1).AsString;
        var jsonPath = GetJsonPathFromString(query);
        var jsonNode = GetJsonNodeFromString(json);

        // Evaluate.
        var pathResult = jsonPath.Evaluate(jsonNode);
        if (pathResult.Matches.Count != 1)
        {
            return VariantValue.Null;
        }
        var result = pathResult.Matches[0];
        if (result.Value == null)
        {
            return VariantValue.Null;
        }
        if (!(result.Value is JsonArray || result.Value is JsonObject))
        {
            return VariantValue.Null;
        }

        // Prepare result.
        var jsonString = result.Value.ToJsonString(_jsonSerializerOptions);
        return new VariantValue(jsonString);
    }

    [SafeFunction]
    [Description("Extracts a scalar value from a JSON string.")]
    [FunctionSignature("json_value(json: string, query: string): any")]
    public static VariantValue JsonValue(FunctionCallInfo args)
    {
        // Parse input.
        var json = args.GetAt(0).AsString;
        var query = args.GetAt(1).AsString;
        var jsonPath = GetJsonPathFromString(query);
        var jsonNode = GetJsonNodeFromString(json);

        // Evaluate.
        var pathResult = jsonPath.Evaluate(jsonNode);
        if (pathResult.Matches.Count != 1)
        {
            return VariantValue.Null;
        }
        var result = pathResult.Matches[0];
        if (result.Value == null)
        {
            return VariantValue.Null;
        }
        if (!(result.Value is JsonValue jsonValue))
        {
            return VariantValue.Null;
        }

        // Prepare result.
        return VariantValue.TryGetValueFromJsonValue(jsonValue, out var value) ? value : VariantValue.Null;
    }

    [SafeFunction]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.SerializeToNode<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.SerializeToNode<TValue>(TValue, JsonSerializerOptions)")]
    [Description("Constructs JSON text from object.")]
    [FunctionSignature("to_json(obj: any): string")]
    public static VariantValue ToJson(FunctionCallInfo args)
    {
        var obj = args.GetAt(0);
        var type = obj.GetInternalType();
        JsonNode? node;
        if (DataTypeUtils.IsSimple(type))
        {
            var dict = new Dictionary<string, object>
            {
                ["value"] = obj.ToString(),
            };
            node = JsonSerializer.SerializeToNode(dict, SourceGenerationContext.Default.DictionaryStringObject);
        }
        else
        {
            node = JsonSerializer.SerializeToNode(obj.AsObject);
        }
        if (node == null)
        {
            return VariantValue.Null;
        }

        var jsonString = node.ToJsonString(_jsonSerializerOptions);
        return new VariantValue(jsonString);
    }

    [SafeFunction]
    [Description("Tests whether a string contains valid JSON.")]
    [FunctionSignature("is_json(json: string): boolean")]
    public static VariantValue IsJson(FunctionCallInfo args)
    {
        var json = args.GetAt(0).AsString;
        try
        {
            JsonDocument.Parse(json);
            return VariantValue.TrueValue;
        }
        catch (JsonException)
        {
            return VariantValue.FalseValue;
        }
    }

    [SafeFunction]
    [Description("Tests whether a JSON path expression returns any SQL/JSON items.")]
    [FunctionSignature("json_exists(json: string, query: string): boolean")]
    public static VariantValue JsonExists(FunctionCallInfo args)
    {
        // Parse input.
        var json = args.GetAt(0).AsString;
        var query = args.GetAt(1).AsString;
        var jsonPath = GetJsonPathFromString(query);
        var jsonNode = GetJsonNodeFromString(json);

        // Evaluate.
        var pathResult = jsonPath.Evaluate(jsonNode);
        return new VariantValue(pathResult.Matches.Count > 0);
    }

    [SafeFunction]
    [Description("Returns the number of elements in the top-level JSON array.")]
    [FunctionSignature("json_array_length(json: string): integer")]
    public static VariantValue JsonArrayLength(FunctionCallInfo args)
    {
        var json = args.GetAt(0).AsString;
        var jsonNode = GetJsonNodeFromString(json);

        if (jsonNode is not JsonArray array)
        {
            return VariantValue.Null;
        }
        return new VariantValue(array.Count);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(JsonQuery);
        functionsManager.RegisterFunction(JsonValue);
        functionsManager.RegisterFunction(ToJson);
        functionsManager.RegisterFunction(IsJson);
        functionsManager.RegisterFunction(JsonExists);
        functionsManager.RegisterFunction(JsonArrayLength);
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false,
    };

    private static JsonNode? GetJsonNodeFromString(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException jsonException)
        {
            throw new QueryCatException(jsonException.Message);
        }
    }

    private static JsonPath GetJsonPathFromString(string query)
    {
        if (!JsonPath.TryParse(query, out var path))
        {
            throw new SemanticException("Incorrect JSON path input.");
        }
        return path;
    }
}
