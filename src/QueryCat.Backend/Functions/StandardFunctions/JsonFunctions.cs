using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Path;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// JSON functions.
/// </summary>
internal static class JsonFunctions
{
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
        if (pathResult.Error?.Length > 0)
        {
            throw new QueryCatException(pathResult.Error[0].ToString());
        }
        if (pathResult.Matches == null || pathResult.Matches.Count != 1)
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
        return new VariantValue(GetJsonStringFromJsonNode(result.Value));
    }

    [Description("Extracts a scalar value from a JSON string.")]
    [FunctionSignature("json_value(json: string, query: string): string")]
    public static VariantValue JsonValue(FunctionCallInfo args)
    {
        // Parse input.
        var json = args.GetAt(0).AsString;
        var query = args.GetAt(1).AsString;
        var jsonPath = GetJsonPathFromString(query);
        var jsonNode = GetJsonNodeFromString(json);

        // Evaluate.
        var pathResult = jsonPath.Evaluate(jsonNode);
        if (pathResult.Error?.Length > 0)
        {
            throw new QueryCatException(pathResult.Error[0].ToString());
        }
        if (pathResult.Matches == null || pathResult.Matches.Count != 1)
        {
            return VariantValue.Null;
        }
        var result = pathResult.Matches[0];
        if (result.Value == null)
        {
            return VariantValue.Null;
        }
        if (!(result.Value is JsonValue))
        {
            return VariantValue.Null;
        }

        // Prepare result.
        return new VariantValue(GetJsonStringFromJsonNode(result.Value));
    }

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
        return new VariantValue(GetJsonStringFromJsonNode(node));
    }

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
        if (pathResult.Error?.Length > 0)
        {
            throw new QueryCatException(pathResult.Error[0].ToString());
        }
        return new VariantValue(pathResult.Matches != null && pathResult.Matches.Count > 0);
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(JsonQuery);
        functionsManager.RegisterFunction(JsonValue);
        functionsManager.RegisterFunction(ToJson);
        functionsManager.RegisterFunction(IsJson);
        functionsManager.RegisterFunction(JsonExists);
    }

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

    private static string GetJsonStringFromJsonNode(JsonNode jsonNode)
    {
        using var ms = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(ms);
        jsonNode.WriteTo(jsonWriter);
        jsonWriter.Flush();
        return new VariantValue(Encoding.UTF8.GetString(ms.ToArray()));
    }
}
