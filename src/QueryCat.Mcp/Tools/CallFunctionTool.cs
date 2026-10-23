using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Commands;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;
using QueryCat.Mcp.Models;

namespace QueryCat.Mcp.Tools;

internal sealed class CallFunctionTool : IMcpTool
{
    private const string NameKey = "name";
    private const string ArgsKey = "args";
    private const string ResultKey = "result";
    private const string ColumnsKey = "columns";
    private const string RowsKey = "rows";
    private const string TypeKey = "type";
    private const string DescriptionKey = "description";

    /// <summary>
    /// Max rows to return for functions that return rows input or rows iterator.
    /// </summary>
    private const int MaxRowsCount = 1000;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(CallFunctionTool));

    /// <inheritdoc />
    public McpToolModel Metadata { get; } = new(
        "call_function",
        "Calls the function from the registry.")
    {
        Description = "Calls the function by name with the specified arguments and returns the result. "
            + "Use search_functions to find the function and its signature first.",
        InputSchema = new McpToolInputSchemaModel
        {
            Properties = new Dictionary<string, McpToolSchemaPropertyModel>
            {
                [NameKey] = new(McpToolSchemaPropertyModel.TypeString,
                    "Function name"),
                [ArgsKey] = new(McpToolSchemaPropertyModel.TypeObject,
                    "Function arguments, object where key is the argument name and value is the argument value"),
            },
            Required = [NameKey],
        },
    };

    /// <inheritdoc />
    public async Task<McpCallToolResultModel> CallAsync(
        IExecutionThread thread,
        IReadOnlyDictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken)
    {
        var name = arguments.TryGetValue(NameKey, out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? (nameElement.GetString() ?? string.Empty).Trim()
            : string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            return McpCallToolResultModel.CreateError(Resources.Errors.McpFunctionNameRequired);
        }

        if (!TryGetCallArguments(arguments, out var callArguments))
        {
            return McpCallToolResultModel.CreateError(Resources.Errors.McpInvalidFunctionArguments);
        }

        JsonElement structuredContent;
        IFunction? function = null;
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Calling function '{FunctionName}' with arguments '{FunctionArguments}'.", name,
                callArguments.ToString());
        }
        try
        {
            function = thread.FunctionsManager.FindByNameFirst(name, callArguments.GetTypes());
            if (function.IsAggregate)
            {
                return McpCallToolResultModel.CreateError(
                    string.Format(Resources.Errors.McpCannotCallAggregateFunction, function.Name));
            }
            var result = await thread.FunctionsManager.CallFunctionAsync(function, thread, callArguments, cancellationToken);
            structuredContent = await CreateStructuredContentAsync(result, cancellationToken);
        }
        catch (SafeModeException e)
        {
            if (function != null)
            {
                return McpCallToolResultModel.CreateError(
                    string.Format(Resources.Errors.McpCannotCallUnsafeFunction, function.Name));
            }
            else
            {
                return McpCallToolResultModel.CreateError(e.Message);
            }
        }
        catch (QueryCatException e)
        {
            return McpCallToolResultModel.CreateError(e.Message);
        }

        return new McpCallToolResultModel
        {
            Content =
            [
                new McpToolTextContentModel
                {
                    Text = structuredContent.ToString(),
                },
            ],
            StructuredContent = structuredContent,
        };
    }

    private static bool TryGetCallArguments(
        IReadOnlyDictionary<string, JsonElement> arguments,
        out FunctionCallArguments callArguments)
    {
        callArguments = new FunctionCallArguments();
        if (!arguments.TryGetValue(ArgsKey, out var argsElement)
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

    private static async Task<JsonElement> CreateStructuredContentAsync(
        VariantValue value,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await using (var jsonWriter = new Utf8JsonWriter(stream))
        {
            if (value.Type == DataType.Object
                && value.AsObjectUnsafe is IRowsInput or IRowsIterator)
            {
                await WriteJsonRowsAsync(jsonWriter, value, cancellationToken);
            }
            else
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName(ResultKey);
                WriteJsonVariantValue(jsonWriter, value);
                jsonWriter.WriteEndObject();
            }
        }
        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Write rows as { "columns": { "name": { "type": "..." } }, "rows": [ { "name": value } ] }.
    /// </summary>
    private static async Task WriteJsonRowsAsync(
        Utf8JsonWriter jsonWriter,
        VariantValue value,
        CancellationToken cancellationToken)
    {
        var rowsInput = value.AsObjectUnsafe as IRowsInput;
        var iterator = RowsIteratorConverter.Convert(value);
        try
        {
            // Some inputs initialize columns only after the first read.
            var hasRow = await iterator.MoveNextAsync(cancellationToken);
            var columnsIndexes = GetVisibleColumnsIndexes(iterator.Columns);

            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName(ColumnsKey);
            jsonWriter.WriteStartObject();
            foreach (var columnIndex in columnsIndexes)
            {
                var column = iterator.Columns[columnIndex];
                jsonWriter.WritePropertyName(column.Name);
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString(TypeKey, column.DataType.ToString().ToLowerInvariant());
                if (!string.IsNullOrEmpty(column.Description))
                {
                    jsonWriter.WriteString(DescriptionKey, column.Description);
                }
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndObject();

            jsonWriter.WritePropertyName(RowsKey);
            jsonWriter.WriteStartArray();
            var rowsCount = 0;
            while (hasRow && rowsCount < MaxRowsCount)
            {
                jsonWriter.WriteStartObject();
                foreach (var columnIndex in columnsIndexes)
                {
                    jsonWriter.WritePropertyName(iterator.Columns[columnIndex].Name);
                    WriteJsonVariantValue(jsonWriter, iterator.Current[columnIndex]);
                }
                jsonWriter.WriteEndObject();
                rowsCount++;
                hasRow = await iterator.MoveNextAsync(cancellationToken);
            }
            jsonWriter.WriteEndArray();

            jsonWriter.WriteEndObject();
        }
        finally
        {
            if (rowsInput != null)
            {
                await rowsInput.CloseAsync(cancellationToken);
            }
        }
    }

    private static int[] GetVisibleColumnsIndexes(Column[] columns)
    {
        var indexes = new List<int>(columns.Length);
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < columns.Length; i++)
        {
            // Skip hidden columns and duplicates, since JSON object keys must be unique.
            if (columns[i].IsHidden || !names.Add(columns[i].Name))
            {
                continue;
            }
            indexes.Add(i);
        }
        return indexes.ToArray();
    }

    private static void WriteJsonVariantValue(Utf8JsonWriter jsonWriter, in VariantValue value)
    {
        if (value.IsNull)
        {
            jsonWriter.WriteNullValue();
            return;
        }

        switch (value.Type)
        {
            case DataType.Integer:
                jsonWriter.WriteNumberValue(value.AsIntegerUnsafe);
                break;
            case DataType.Float:
                jsonWriter.WriteNumberValue(value.AsFloatUnsafe);
                break;
            case DataType.Numeric:
                jsonWriter.WriteNumberValue(value.AsNumericUnsafe);
                break;
            case DataType.String:
                jsonWriter.WriteStringValue(value.AsStringUnsafe);
                break;
            case DataType.Boolean:
                jsonWriter.WriteBooleanValue(value.AsBooleanUnsafe);
                break;
            case DataType.Array:
                jsonWriter.WriteStartArray();
                foreach (var item in value.AsArrayUnsafe)
                {
                    WriteJsonVariantValue(jsonWriter, item);
                }
                jsonWriter.WriteEndArray();
                break;
            case DataType.Map:
                jsonWriter.WriteStartObject();
                foreach (var (key, item) in value.AsMapUnsafe)
                {
                    jsonWriter.WritePropertyName(key.ToString(CultureInfo.InvariantCulture));
                    WriteJsonVariantValue(jsonWriter, item);
                }
                jsonWriter.WriteEndObject();
                break;
            default:
                jsonWriter.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
                break;
        }
    }
}
