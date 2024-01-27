using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Addons.Formatters;

internal sealed class JsonOutput : RowsOutput, IDisposable
{
    private readonly Utf8JsonWriter _streamWriter;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(JsonOutput));

    public JsonOutput(Stream stream)
    {
        _streamWriter = new Utf8JsonWriter(stream);
    }

    /// <inheritdoc />
    public override void Open()
    {
        _logger.LogTrace("JSON opened.");
    }

    /// <inheritdoc />
    public override void Close()
    {
        _streamWriter.WriteEndArray();
        _streamWriter.Flush();
        _streamWriter.Dispose();
        _logger.LogTrace("JSON closed.");
    }

    /// <inheritdoc />
    protected override void OnWrite(in VariantValue[] values)
    {
        _streamWriter.WriteStartObject();
        var columns = QueryContext.QueryInfo.Columns;
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }
            _streamWriter.WritePropertyName(columns[i].Name);
            WriteJsonVariantValue(_streamWriter, values[i]);
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
