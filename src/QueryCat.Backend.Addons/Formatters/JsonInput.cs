using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Path;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
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
    public JsonInput(StreamReader streamReader, bool addFileNameColumn = true, string? jsonPath = null, string? key = null)
        : base(
            GetEvaluatedStream(streamReader, jsonPath), new StreamRowsInputOptions
        {
            DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = ['{', '}'],
                QuoteChars = ['"'],
                SkipEmptyLines = true,
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

    private static StreamReader GetEvaluatedStream(StreamReader streamReader, string? jsonPath = null)
    {
        if (string.IsNullOrEmpty(jsonPath))
        {
            return streamReader;
        }

        JsonNode? jsonNode;
        try
        {
            jsonNode = JsonNode.Parse(streamReader.BaseStream);
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
        return new StreamReader(ms);
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
            if (hasData)
            {
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
            }
            return hasData;
        }
    }

    /// <inheritdoc />
    protected override void SetDefaultColumns(int columnsCount)
    {
        var jsonElement = GetParsedJsonDocument();

        var list = new List<string>();
        foreach (var field in GetJsonObjectFields(jsonElement))
        {
            list.Add(field);
        }
        _properties = list.ToArray();

        base.SetDefaultColumns(_properties.Length);
    }

    /// <inheritdoc />
    protected override async Task AnalyzeAsync(CacheRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        var columns = GetInputColumns();
        for (var i = 0; i < _properties.Length; i++)
        {
            columns[i].Name = _properties[i];
        }
        await RowsIteratorUtils.ResolveColumnsTypesAsync(iterator, cancellationToken: cancellationToken);
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
            yield return jsonProperty.Name;
        }
    }
}
