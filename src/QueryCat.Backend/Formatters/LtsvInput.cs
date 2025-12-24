using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

internal sealed class LtsvInput : StreamRowsInput
{
    private string[] _values = [];
    private readonly Dictionary<string, string> _additionalValues = new();
    private int _virtualColumnsCount = 0;

    /// <inheritdoc />
    public LtsvInput(Stream stream, bool addFileNameColumn = true, string? key = null)
        : base(stream, new StreamRowsInputOptions
        {
            DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = ['\t'],
                QuoteChars = ['"'],
                SkipEmptyLines = true,
                EnableQuotesModeOnFieldStart = false,
                Culture = Application.Culture,
            },
            AddInputSourceColumn = addFileNameColumn,
        }, key ?? string.Empty)
    {
    }

    /// <inheritdoc />
    protected override async Task<Column[]> InitializeColumnsAsync(
        IRowsInput input,
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

            foreach (var additionalValue in _additionalValues)
            {
                list.Add(additionalValue.Key);
            }
        }

        var columns = list.Select(l => new Column(l, DataType.String)).ToArray();
        _values = new string[columns.Length];
        _virtualColumnsCount = GetVirtualColumns().Length;
        return columns;
    }

    /// <inheritdoc />
    protected override ErrorCode ReadValueInternal(int nonVirtualColumnIndex, DataType type, out VariantValue value)
    {
        if (nonVirtualColumnIndex < _values.Length)
        {
            var errorCode = VariantValue.TryCreateFromString(
                _values[nonVirtualColumnIndex],
                type,
                out value)
                ? ErrorCode.OK : ErrorCode.CannotCast;
            return errorCode;
        }

        value = VariantValue.Null;
        return ErrorCode.NoData;
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> ReadNextInternalAsync(CancellationToken cancellationToken)
    {
        var hasData = await base.ReadNextInternalAsync(cancellationToken);
        if (!hasData)
        {
            return hasData;
        }

        Array.Clear(_values);
        _additionalValues.Clear();

        var columnsCount = GetInputColumnsCount();
        var spanBuffer = new Span<Range>(new Range[2]);
        for (var i = 0; i < columnsCount; i++)
        {
            var item = GetInputColumnValue(i);
            if (item.Split(spanBuffer, ':', StringSplitOptions.TrimEntries) < 2)
            {
                continue;
            }
            var columnHeader = Unquote(item[spanBuffer[0]]).ToString();
            if (columnHeader.Length < 1)
            {
                continue;
            }
            var columnValue = Unquote(item[spanBuffer[1]]).ToString();
            var columnIndex = this.GetColumnIndexByName(columnHeader) - _virtualColumnsCount;
            if (columnIndex < 0)
            {
                _additionalValues[columnHeader] = columnValue;
            }
            else
            {
                _values[columnIndex] = columnValue;
            }
        }
        return hasData;
    }

    private ReadOnlySpan<char> Unquote(ReadOnlySpan<char> span)
    {
        if (span.StartsWith("\""))
        {
            span = StringUtils.Unquote(span, "\"");
        }
        if (span.StartsWith("'"))
        {
            span = StringUtils.Unquote(span, "'");
        }
        return span;
    }
}
