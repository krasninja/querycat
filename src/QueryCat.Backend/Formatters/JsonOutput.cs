using System.Text.Json;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

internal sealed class JsonOutput : RowsOutput, IDisposable
{
    private readonly Utf8JsonWriter _streamWriter;

    public JsonOutput(Stream stream)
    {
        _streamWriter = new Utf8JsonWriter(stream);
    }

    /// <inheritdoc />
    public override void Open()
    {
        Logger.Instance.Trace("Opened.", nameof(JsonOutput));
    }

    /// <inheritdoc />
    public override void Close()
    {
        _streamWriter.WriteEndArray();
        _streamWriter.Flush();
        _streamWriter.Dispose();
        Logger.Instance.Trace("Closed.", nameof(JsonOutput));
    }

    /// <inheritdoc />
    protected override void OnWrite(Row row)
    {
        _streamWriter.WriteStartObject();
        for (var i = 0; i < row.Columns.Length; i++)
        {
            if (row.Columns[i].IsHidden)
            {
                continue;
            }
            _streamWriter.WritePropertyName(row.Columns[i].Name);
            WriteJsonVariantValue(_streamWriter, row[i]);
        }
        _streamWriter.WriteEndObject();
        _streamWriter.Flush();
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
        _streamWriter.WriteStartArray();
    }

    private static void WriteJsonVariantValue(Utf8JsonWriter jsonWriter, in VariantValue value)
    {
        if (value.IsNull)
        {
            jsonWriter.WriteNullValue();
            return;
        }

        switch (value.GetInternalType())
        {
            case DataType.Integer:
                jsonWriter.WriteNumberValue(value.AsInteger);
                break;
            case DataType.Float:
                jsonWriter.WriteNumberValue(value.AsFloat);
                break;
            case DataType.Numeric:
                jsonWriter.WriteNumberValue(value.AsNumeric);
                break;
            case DataType.String:
                jsonWriter.WriteStringValue(value.AsString);
                break;
            case DataType.Boolean:
                jsonWriter.WriteBooleanValue(value.AsBoolean);
                break;
            default:
                jsonWriter.WriteStringValue(value.ToString());
                break;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _streamWriter.Dispose();
    }
}
