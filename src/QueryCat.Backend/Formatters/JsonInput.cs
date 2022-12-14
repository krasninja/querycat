using System.Buffers;
using System.Text.Json;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Input that parser JSON data.
/// </summary>
internal sealed class JsonInput : StreamRowsInput
{
    private int _bracketsCount;

    private JsonDocument? _jsonDocument;
    private string[] _properties = Array.Empty<string>();

    /// <inheritdoc />
    public JsonInput(StreamReader streamReader, bool addFileNameColumn = true) : base(streamReader, new StreamRowsInputOptions
    {
        DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
        {
            Delimiters = new[] { '{', '}' },
            QuoteChars = new[] { '"' },
            SkipEmptyLines = true,
            CompleteOnEndOfLine = false,
            QuotesEscapeStyle = DelimiterStreamReader.QuotesMode.Backslash,
            IncludeDelimiter = true,
        },
        AddInputSourceColumn = addFileNameColumn,
    })
    {
        SetOnDelimiterDelegate(OnDelimiter);
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
        if (_jsonDocument == null)
        {
            value = VariantValue.Null;
            return ErrorCode.Error;
        }

        if (!_jsonDocument.RootElement.TryGetProperty(_properties[nonVirtualColumnIndex], out var property))
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
    protected override bool ReadNextInternal()
    {
        var hasData = base.ReadNextInternal();
        if (hasData)
        {
            _jsonDocument?.Dispose();
            var text = GetRowText();
            var reader = new SequenceReader<char>(text);
            if (reader.TryAdvanceTo('{', advancePastDelimiter: false))
            {
                var json = reader.UnreadSequence.ToString();
                _jsonDocument = JsonSerializer.Deserialize<JsonDocument>(json);
            }
            else
            {
                hasData = false;
            }
        }
        return hasData;
    }

    /// <inheritdoc />
    protected override void SetDefaultColumns(int columnsCount)
    {
        using var jsonDocument = GetParsedJsonDocument();
        if (jsonDocument == null)
        {
            throw new InvalidOperationException("Cannot initialize JSON columns.");
        }

        var list = new List<string>();
        foreach (var jsonProperty in jsonDocument.RootElement.EnumerateObject())
        {
            list.Add(jsonProperty.Name);
        }
        _properties = list.ToArray();

        base.SetDefaultColumns(_properties.Length);
    }

    /// <inheritdoc />
    protected override void Analyze(CacheRowsIterator iterator)
    {
        var columns = GetInputColumns();
        for (var i = 0; i < _properties.Length; i++)
        {
            columns[i].Name = _properties[i];
        }
        RowsIteratorUtils.ResolveColumnsTypes(iterator);
    }

    private JsonDocument? GetParsedJsonDocument()
    {
        var text = GetRowText();
        var reader = new SequenceReader<char>(text);
        if (reader.TryAdvanceTo('{', advancePastDelimiter: false))
        {
            var json = reader.UnreadSequence.ToString();
            return JsonSerializer.Deserialize<JsonDocument>(json);
        }
        return null;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _jsonDocument?.Dispose();
        }
        base.Dispose(disposing);
    }
}
