using System.Globalization;
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

    public JsonOutput(Stream stream, int? indent = null)
    {
        var options = default(JsonWriterOptions);
        if (indent.HasValue)
        {
            options.IndentSize = indent.Value;
            options.Indented = true;
        }
        _streamWriter = new Utf8JsonWriter(stream, options);
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("JSON opened.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _streamWriter.WriteEndArray();
        await _streamWriter.FlushAsync(cancellationToken);
        await _streamWriter.DisposeAsync();
        _logger.LogTrace("JSON closed.");
    }

    /// <inheritdoc />
    protected override async ValueTask<ErrorCode> OnWriteAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        _streamWriter.WriteStartObject();
        var columns = QueryContext.QueryInfo.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }
            _streamWriter.WritePropertyName(columns[i].Name);
            WriteJsonVariantValue(_streamWriter, values[i]);
        }
        _streamWriter.WriteEndObject();
        await _streamWriter.FlushAsync(cancellationToken);

        return ErrorCode.OK;
    }

    /// <inheritdoc />
    protected override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _streamWriter.WriteStartArray();
        return Task.CompletedTask;
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
                jsonWriter.WriteStringValue(value.AsString);
                break;
            case DataType.Boolean:
                jsonWriter.WriteBooleanValue(value.AsBoolean);
                break;
            default:
                jsonWriter.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
                break;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _streamWriter.Dispose();
    }
}
