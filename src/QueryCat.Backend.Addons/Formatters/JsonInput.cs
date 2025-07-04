using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Path;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// Input that parser JSON data.
/// </summary>
internal class JsonInput : StreamRowsInput
{
    private int _bracketsCount;

    private JsonElement? _jsonElement;
    private string[] _properties = [];

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(JsonInput));

    /// <inheritdoc />
    public JsonInput(Stream stream, bool addFileNameColumn = true, string? jsonPath = null, string? key = null)
        : base(GetEvaluatedStream(stream, jsonPath), new StreamRowsInputOptions
        {
            DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = ['{', '}'],
                QuoteChars = ['"'],
                SkipEmptyLines = true,
                EnableQuotesModeOnFieldStart = false,
                CompleteOnEndOfLine = false,
                QuotesEscapeStyle = DelimiterStreamReader.QuotesMode.Backslash,
                IncludeDelimiter = true,
                Culture = Application.Culture,
            },
            AddInputSourceColumn = addFileNameColumn,
        }, key ?? string.Empty)
    {
        SetOnDelimiterDelegate(OnDelimiter);
    }

    private static Stream GetEvaluatedStream(Stream stream, string? jsonPath = null)
    {
        if (string.IsNullOrEmpty(jsonPath))
        {
            return stream;
        }

        JsonNode? jsonNode;
        try
        {
            jsonNode = JsonNode.Parse(stream);
        }
        catch (JsonException ex)
        {
            throw new QueryCatException($"Invalid JSON: {ex.Message}");
        }
        if (jsonNode == null)
        {
            throw new QueryCatException("Invalid JSON.");
        }

        if (!JsonPath.TryParse(jsonPath, out var path))
        {
            throw new SemanticException("Incorrect JSON path input.");
        }
        var pathResult = path.Evaluate(jsonNode);

        var ms = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(ms);
        jsonWriter.WriteStartArray();
        foreach (var match in pathResult.Matches)
        {
            if (match.Value == null)
            {
                continue;
            }
            match.Value.WriteTo(jsonWriter);
        }
        jsonWriter.WriteEndArray();
        jsonWriter.Flush();
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    private void OnDelimiter(char ch, long pos, out bool countField, out bool completeLine)
    {
        // Count "{" and "}" brackets until we find the complete JSON object.
        if (ch == '{')
        {
            _bracketsCount++;
        }
        else if (ch == '}')
        {
            _bracketsCount--;
        }

        completeLine = _bracketsCount == 0;
        countField = completeLine || _bracketsCount == 1;
    }

    /// <inheritdoc />
    protected override ErrorCode ReadValueInternal(int nonVirtualColumnIndex, DataType type, out VariantValue value)
    {
        if (_jsonElement == null)
        {
            value = VariantValue.Null;
            return ErrorCode.NoData;
        }

        if (!_jsonElement.Value.TryGetProperty(_properties[nonVirtualColumnIndex], out var property))
        {
            value = VariantValue.Null;
            return ErrorCode.OK;
        }
        if (property.ValueKind == JsonValueKind.Number && DataTypeUtils.IsNumeric(type))
        {
            if (property.TryGetInt64(out var longValue))
            {
                value = new VariantValue(longValue);
                return ErrorCode.OK;
            }
            if (property.TryGetDouble(out var doubleValue))
            {
                value = new VariantValue(doubleValue);
                return ErrorCode.OK;
            }
        }
        else if (property.ValueKind == JsonValueKind.String)
        {
            if (type == DataType.Timestamp && property.TryGetDateTime(out var dateTimeValue))
            {
                value = new VariantValue(dateTimeValue);
                return ErrorCode.OK;
            }
            if (type == DataType.String)
            {
                value = new VariantValue(property.GetString());
                return ErrorCode.OK;
            }
        }
        if (property.ValueKind == JsonValueKind.Number && type == DataType.Interval
            && property.TryGetInt64(out var intervalValue))
        {
            value = new VariantValue(TimeSpan.FromTicks(intervalValue));
            return ErrorCode.OK;
        }
        if (type == DataType.String)
        {
            value = new VariantValue(property.GetRawText());
            return ErrorCode.OK;
        }

        value = VariantValue.Null;
        return ErrorCode.CannotCast;
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> ReadNextInternalAsync(CancellationToken cancellationToken)
    {
        // Read until success.
        while (true)
        {
            var hasData = await base.ReadNextInternalAsync(cancellationToken);
            if (!hasData)
            {
                return false;
            }
            var text = GetRowText();
            var reader = new SequenceReader<char>(text);
            if (reader.TryAdvanceTo('{', advancePastDelimiter: false))
            {
                var json = reader.UnreadSequence.ToString();
                try
                {
                    _jsonElement = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.JsonElement);
                }
                catch (JsonException jsonException)
                {
                    _logger.LogWarning("Cannot parse row {RowIndex}: {Error}", RowIndex, jsonException.Message);
                    continue;
                }
            }
            else
            {
                hasData = false;
            }
            return hasData;
        }
    }

    /// <inheritdoc />
    protected override async Task<Column[]> InitializeColumnsAsync(IRowsInput input,
        CancellationToken cancellationToken = default)
    {
        var list = new HashSet<string>();

        for (var i = 0; i < QueryContext.PrereadRowsCount; i++)
        {
            var hasData = await input.ReadNextAsync(cancellationToken);
            if (!hasData)
            {
                break;
            }

            var jsonElement = GetParsedJsonDocument();
            foreach (var field in GetJsonObjectFields(jsonElement))
            {
                list.Add(field);
            }
        }

        _properties = list.ToArray();
        var columns = _properties.Select(p => new Column(p, DataType.String));
        return columns.ToArray();
    }

    private JsonElement? GetParsedJsonDocument()
    {
        var text = GetRowText();
        var reader = new SequenceReader<char>(text);
        if (reader.TryAdvanceTo('{', advancePastDelimiter: false))
        {
            var json = reader.UnreadSequence.ToString();
            return JsonSerializer.Deserialize(json, SourceGenerationContext.Default.JsonElement);
        }
        return null;
    }

    private IEnumerable<string> GetJsonObjectFields(JsonElement? jsonElement)
    {
        if (jsonElement == null)
        {
            yield break;
        }

        foreach (var jsonProperty in jsonElement.Value.EnumerateObject())
        {
            if (string.IsNullOrEmpty(jsonProperty.Name))
            {
                continue;
            }
            yield return jsonProperty.Name;
        }
    }
}
