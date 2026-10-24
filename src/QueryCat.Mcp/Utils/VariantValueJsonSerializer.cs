using System.Globalization;
using System.Text.Json;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Mcp.Utils;

internal static class VariantValueJsonSerializer
{
    internal const string ResultKey = "result";
    internal const string ColumnsKey = "columns";
    internal const string RowsKey = "rows";
    internal const string TypeKey = "type";
    internal const string DescriptionKey = "description";

    /// <summary>
    /// Max rows to return for functions that return rows input or rows iterator.
    /// </summary>
    internal const int MaxRowsCount = 1000;

    internal static async Task<JsonElement> CreateStructuredContentAsync(
        VariantValue value,
        int maxRows = MaxRowsCount,
        CancellationToken cancellationToken = default)
    {
        maxRows = Math.Clamp(maxRows, 0, MaxRowsCount);
        using var stream = new MemoryStream();
        await using (var jsonWriter = new Utf8JsonWriter(stream))
        {
            if (value.Type == DataType.Object
                && value.AsObjectUnsafe is IRowsInput or IRowsIterator)
            {
                await WriteJsonRowsAsync(jsonWriter, value, maxRows, cancellationToken);
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
        int maxRows,
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
            while (hasRow && rowsCount < maxRows)
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
