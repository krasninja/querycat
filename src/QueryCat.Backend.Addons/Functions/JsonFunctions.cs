using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Path;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Addons.Functions;

/// <summary>
/// JSON functions.
/// </summary>
public static class JsonFunctions
{
    [SafeFunction]
    [Description("Extracts an object or an array from a JSON string.")]
    [FunctionSignature("json_query(json: string, query: string): string")]
    public static VariantValue JsonQuery(IExecutionThread thread)
    {
        // Parse input.
        var json = thread.Stack[0].AsString;
        var query = thread.Stack[1].AsString;
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
    public static VariantValue JsonValue(IExecutionThread thread)
    {
        // Parse input.
        var json = thread.Stack[0].AsString;
        var query = thread.Stack[1].AsString;
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
    [Description("Constructs JSON text from object.")]
    [FunctionSignature("to_json(obj: any): string")]
    public static VariantValue ToJson(IExecutionThread thread)
    {
        var obj = thread.Stack.Pop();
        var node = ConvertToJsonNode(in obj);
        if (node == null)
        {
            return new VariantValue("null");
        }

        var jsonString = node.ToJsonString(_jsonSerializerOptions);
        return new VariantValue(jsonString);
    }

    #region JSON convert

    private static JsonNode? ConvertToJsonNode(in VariantValue value)
    {
        return value.Type switch
        {
            DataType.Null or DataType.Void => null,
            DataType.Integer => System.Text.Json.Nodes.JsonValue.Create(value.AsIntegerUnsafe),
            DataType.Float => System.Text.Json.Nodes.JsonValue.Create(value.AsFloatUnsafe),
            DataType.Numeric => System.Text.Json.Nodes.JsonValue.Create(value.AsNumericUnsafe),
            DataType.String => System.Text.Json.Nodes.JsonValue.Create(value.AsStringUnsafe),
            DataType.Boolean => System.Text.Json.Nodes.JsonValue.Create(value.AsBooleanUnsafe),
            DataType.Timestamp => System.Text.Json.Nodes.JsonValue.Create(value.AsTimestampUnsafe.ToString("O")),
            DataType.Interval => System.Text.Json.Nodes.JsonValue.Create(value.AsIntervalUnsafe.ToString()),
            DataType.Blob => ConvertBlobToJsonNode(value.AsBlobUnsafe),
            DataType.Array => ConvertArrayToJsonArray(value.AsArrayUnsafe),
            DataType.Map => ConvertMapToJsonObject(value.AsMapUnsafe),
            DataType.Object => ConvertObjectToJsonNode(value.AsObjectUnsafe),
            _ => null,
        };
    }

    private static JsonNode ConvertBlobToJsonNode(IBlobData blob)
    {
        using var stream = blob.GetStream();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return System.Text.Json.Nodes.JsonValue.Create(Convert.ToBase64String(memoryStream.ToArray()));
    }

    private static JsonArray ConvertArrayToJsonArray(IEnumerable<VariantValue> list)
    {
        var jsonArray = new JsonArray();
        foreach (var item in list)
        {
            jsonArray.Add(ConvertToJsonNode(in item));
        }
        return jsonArray;
    }

    private static JsonObject ConvertMapToJsonObject(IDictionary<VariantValue, VariantValue> map)
    {
        var jsonObject = new JsonObject();
        foreach (var kvp in map)
        {
            var key = kvp.Key.ToString(CultureInfo.InvariantCulture);
            var val = kvp.Value;
            jsonObject[key] = ConvertToJsonNode(in val);
        }
        return jsonObject;
    }

    private static JsonNode? ConvertObjectToJsonNode(object? obj)
    {
        if (obj == null)
        {
            return null;
        }

        // Handle JsonNode directly.
        if (obj is JsonNode jsonNode)
        {
            return jsonNode;
        }
        // Handle IDictionary<VariantValue, VariantValue>.
        if (obj is IDictionary<VariantValue, VariantValue> variantMap)
        {
            return ConvertMapToJsonObject(variantMap);
        }
        // Handle IDictionary<string, VariantValue>.
        if (obj is IDictionary<string, VariantValue> stringVariantMap)
        {
            var jsonObject = new JsonObject();
            foreach (var kvp in stringVariantMap)
            {
                var val = kvp.Value;
                jsonObject[kvp.Key] = ConvertToJsonNode(in val);
            }
            return jsonObject;
        }
        // Handle IList<VariantValue>.
        if (obj is IList<VariantValue> variantList)
        {
            return ConvertArrayToJsonArray(variantList);
        }
        // Handle VariantValue[].
        if (obj is VariantValue[] variantArray)
        {
            return ConvertArrayToJsonArray(variantArray);
        }
        // Handle IEnumerable<VariantValue>.
        if (obj is IEnumerable<VariantValue> variantEnumerable)
        {
            return ConvertArrayToJsonArray(variantEnumerable);
        }

        // Fallback: convert to string.
        return System.Text.Json.Nodes.JsonValue.Create(obj.ToString());
    }

    #endregion

    [SafeFunction]
    [Description("Tests whether a string contains valid JSON.")]
    [FunctionSignature("is_json(json: string): boolean")]
    public static VariantValue IsJson(IExecutionThread thread)
    {
        var json = thread.Stack.Pop();
        try
        {
            using var doc = JsonDocument.Parse(json);
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
    public static VariantValue JsonExists(IExecutionThread thread)
    {
        // Parse input.
        var json = thread.Stack[0].AsString;
        var query = thread.Stack[1].AsString;
        var jsonPath = GetJsonPathFromString(query);
        var jsonNode = GetJsonNodeFromString(json);

        // Evaluate.
        var pathResult = jsonPath.Evaluate(jsonNode);
        return new VariantValue(pathResult.Matches.Count > 0);
    }

    [SafeFunction]
    [Description("Expands the top-level JSON array into a set of values.")]
    [FunctionSignature("json_array_elements(json: string): object<IRowsIterator>")]
    [FunctionSignature("json_array_elements(json: array): object<IRowsIterator>")]
    public static VariantValue JsonArrayElements(IExecutionThread thread)
    {
        var json = thread.Stack.Pop();
        if (json.Type == DataType.Array)
        {
            return VariantValue.CreateFromObject(new ListRowsIterator(json.AsArrayUnsafe.AsReadOnly()));
        }

        var jsonNode = GetJsonNodeFromString(json);

        if (jsonNode is not JsonArray array)
        {
            return VariantValue.Null;
        }

        var values = array.Select(item => VariantValue.CreateFromObject(item)).ToList();
        var iterator = new ListRowsIterator(values);
        return VariantValue.CreateFromObject(iterator);
    }

    [SafeFunction]
    [Description("Returns the number of elements in the top-level JSON array.")]
    [FunctionSignature("json_array_length(json: string): integer")]
    [FunctionSignature("json_array_length(json: array): integer")]
    public static VariantValue JsonArrayLength(IExecutionThread thread)
    {
        var json = thread.Stack.Pop();
        if (json.Type == DataType.Array)
        {
            return new VariantValue(json.AsArrayUnsafe.Count);
        }

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
        functionsManager.RegisterFunction(JsonArrayElements);
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
