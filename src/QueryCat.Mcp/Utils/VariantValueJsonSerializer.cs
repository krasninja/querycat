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
    internal const string TruncatedKey = "truncated";

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
    /// Write rows as { "columns": { "name": { "type": "..." } }, "rows": [ { "name": value } ], "truncated": false }.
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
            var (columnsIndexes, columnsNames) = GetVisibleColumns(iterator.Columns);

            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName(ColumnsKey);
            jsonWriter.WriteStartObject();
            for (var i = 0; i < columnsIndexes.Length; i++)
            {
                var column = iterator.Columns[columnsIndexes[i]];
                jsonWriter.WritePropertyName(columnsNames[i]);
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
                for (var i = 0; i < columnsIndexes.Length; i++)
                {
                    jsonWriter.WritePropertyName(columnsNames[i]);
                    WriteJsonVariantValue(jsonWriter, iterator.Current[columnsIndexes[i]]);
                }
                jsonWriter.WriteEndObject();
                rowsCount++;
                hasRow = await iterator.MoveNextAsync(cancellationToken);
            }
            jsonWriter.WriteEndArray();

            // The rows limit has been reached, but there are more rows.
            jsonWriter.WriteBoolean(TruncatedKey, hasRow);

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

    private static (int[] Indexes, string[] Names) GetVisibleColumns(Column[] columns)
    {
        var indexes = new List<int>(columns.Length);
        var names = new List<string>(columns.Length);
        var uniqueNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            // JSON object keys must be unique, add suffix to duplicates (id, id_1, id_2).
            var name = columns[i].Name;
            var suffix = 1;
            while (!uniqueNames.Add(name))
            {
                name = $"{columns[i].Name}_{suffix++}";
            }
            indexes.Add(i);
            names.Add(name);
        }
        return (indexes.ToArray(), names.ToArray());
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
                var floatValue = value.AsFloatUnsafe;
                // JSON does not support NaN and infinity.
                if (double.IsFinite(floatValue))
                {
                    jsonWriter.WriteNumberValue(floatValue);
                }
                else
                {
                    jsonWriter.WriteStringValue(floatValue.ToString(CultureInfo.InvariantCulture));
                }
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
