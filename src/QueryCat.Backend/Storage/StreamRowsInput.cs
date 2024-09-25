using System.Buffers;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The base class that contains common logic to parse text data from a stream. With
/// additional functionality:
/// - analyze text data schema;
/// - cache analyze data to prevent double reading;
/// - IRowsInput interface implementation;
/// - StreamReader reset (if Stream supports Seek);
/// - virtual (custom) columns.
/// </summary>
public abstract class StreamRowsInput : IRowsInput, IDisposable
{
    /*
     * The main workflow is:
     * - Open()
     *   - ReadNext() - get first row
     *     - ReadNextInternal()
     *     - SetDefaultColumns() - on this stage we know the real columns count, pre-initialize
     *   - Analyze() - init columns, add custom columns, resolve columns types
     * - SetContext()
     * - ReadNext() - real data reading, cache first, stream next
     * - Close()
     */

    private readonly StreamRowsInputOptions _options;

    private readonly Column[] _customColumns =
    {
        new("filename", DataType.String, "File path."), // Index 0.
    };

    private int _rowIndex;

    /// <summary>
    /// Row index.
    /// </summary>
    protected int RowIndex => _rowIndex;

    protected StreamReader StreamReader { get; }

    private bool _isClosed;

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    // Current position in a reading row. We need it in a case if read current row and need to
    // fetch new data to finish. The current position will contain the start index.
    private bool _firstFetch = true;

    // Cache.
    private CacheRowsIterator? _cacheIterator;

    private Column[] _columns = [];

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public string[] UniqueKey { get; set; }

    internal bool DetectColumnsTypes { get; set; } = true;

    private int _virtualColumnsCount;

    private readonly DelimiterStreamReader _delimiterStreamReader;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(StreamRowsInput));

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="streamReader">Stream reader.</param>
    /// <param name="options">The options.</param>
    /// <param name="keys">Unique keys.</param>
    protected StreamRowsInput(
        StreamReader streamReader,
        StreamRowsInputOptions? options = null,
        params string[] keys)
    {
        _options = options ?? new();
        _delimiterStreamReader = new DelimiterStreamReader(streamReader, _options.DelimiterStreamReaderOptions);
        UniqueKey = keys.Concat(_options.CacheKeys).ToArray();
        StreamReader = streamReader;
    }

    /// <summary>
    /// Set the OnDelimiterDelegate delegate value to override parsing behavior.
    /// </summary>
    /// <param name="onDelimiterDelegate">Delegate.</param>
    protected void SetOnDelimiterDelegate(DelimiterStreamReader.OnDelimiterDelegate onDelimiterDelegate)
    {
        _delimiterStreamReader.OnDelimiter = onDelimiterDelegate;
    }

    /// <inheritdoc />
    public void Close()
    {
        _logger.LogDebug("Close {Stream}.", this);
        Dispose(true);
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (TryReadVirtualOrCachedColumnValue(columnIndex, out value, out var errorCode))
        {
            return errorCode;
        }

        var nonVirtualColumnIndex = columnIndex - _virtualColumnsCount;
        var type = Columns[columnIndex].DataType;
        return ReadValueInternal(nonVirtualColumnIndex, type, out value);
    }

    /// <summary>
    /// Actual value reading. It is called when there is no cached data.
    /// </summary>
    /// <param name="nonVirtualColumnIndex">Actual column index to read.</param>
    /// <param name="type">Column data type.</param>
    /// <param name="value">Return value.</param>
    /// <returns>Error code.</returns>
    protected virtual ErrorCode ReadValueInternal(int nonVirtualColumnIndex, DataType type, out VariantValue value)
    {
        var errorCode = VariantValue.TryCreateFromString(
            _delimiterStreamReader.GetField(nonVirtualColumnIndex),
            type,
            out value)
            ? ErrorCode.OK : ErrorCode.CannotCast;
        return errorCode;
    }

    private bool TryReadVirtualOrCachedColumnValue(int columnIndex, out VariantValue value, out ErrorCode errorCode)
    {
        string stringValue;
        if (_cacheIterator != null)
        {
            stringValue = _cacheIterator.Current[columnIndex].AsString;
        }
        else if (columnIndex < _virtualColumnsCount)
        {
            stringValue = GetVirtualColumnValue(_rowIndex, columnIndex).AsString;
        }
        else
        {
            value = VariantValue.Null;
            errorCode = ErrorCode.OK;
            return false;
        }

        errorCode = VariantValue.TryCreateFromString(
            stringValue,
            Columns[columnIndex].DataType,
            out value)
            ? ErrorCode.OK : ErrorCode.Error;
        return true;
    }

    /// <summary>
    /// Set pre initialized columns.
    /// </summary>
    /// <param name="columnsCount">Columns count.</param>
    protected virtual void SetDefaultColumns(int columnsCount)
    {
        var newColumns = new Column[columnsCount];
        for (var i = 0; i < newColumns.Length; i++)
        {
            newColumns[i] = new Column(i, DataType.String);
        }
        SetColumns(newColumns);
    }

    /// <summary>
    /// Initialize columns with the new set.
    /// </summary>
    /// <param name="columns">New columns.</param>
    protected void SetColumns(IReadOnlyList<Column> columns)
    {
        if (columns.Count < 1)
        {
            throw new QueryCatException(Resources.Errors.NoColumns);
        }

        var virtualColumns = GetVirtualColumns();
        _virtualColumnsCount = virtualColumns.Length;
        var nonVirtualColumns = columns.Where(c => !virtualColumns.Contains(c)).ToArray();
        Array.Resize(ref _columns, nonVirtualColumns.Length + _virtualColumnsCount);

        // Add virtual columns.
        for (var i = 0; i < _virtualColumnsCount; i++)
        {
            _columns[i] = new Column(virtualColumns[i]);
        }

        // Add non-virtual columns.
        for (var i = _virtualColumnsCount; i < nonVirtualColumns.Length + _virtualColumnsCount; i++)
        {
            _columns[i] = nonVirtualColumns[i - _virtualColumnsCount];
        }
    }

    /// <inheritdoc />
    public virtual bool ReadNext()
    {
        bool hasData;
        do
        {
            hasData = false;

            // If we have cached data - return it first.
            if (_cacheIterator != null)
            {
                if (_cacheIterator.MoveNext())
                {
                    hasData = true;
                }
                else
                {
                    _cacheIterator = null;
                }
            }
            if (!hasData)
            {
                if (_isClosed)
                {
                    return false;
                }
                hasData = ReadNextInternal();
            }
            if (hasData)
            {
                _rowIndex++;
            }

            if (hasData && _firstFetch)
            {
                SetDefaultColumns(_delimiterStreamReader.GetFieldsCount());
                _firstFetch = false;
            }
        }
        while (hasData && IgnoreLine());
        return hasData;
    }

    /// <summary>
    /// Actual next data reading.
    /// </summary>
    /// <returns><c>True</c> if has data, <c>false</c> otherwise.</returns>
    protected virtual bool ReadNextInternal()
    {
        return _delimiterStreamReader.Read();
    }

    /// <inheritdoc />
    public virtual void Reset()
    {
        _logger.LogTrace("Reset stream.");
        // If we still read cache data - we just reset it. Otherwise, there will be double read.
        if (_cacheIterator != null)
        {
            _cacheIterator.SeekToHead();
        }
        else
        {
            StreamReader.DiscardBufferedData();
            StreamReader.BaseStream.Seek(0, SeekOrigin.Begin);
            _delimiterStreamReader.Reset();
        }
        _rowIndex = 0;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine(this.ToString());
    }

    /// <summary>
    /// Should return <c>true</c> if the current row must be ignored
    /// from processing. Return <c>false</c> otherwise.
    /// </summary>
    /// <returns><c>True</c> if should skip, <c>false</c> otherwise.</returns>
    protected virtual bool IgnoreLine() => false;

    /// <summary>
    /// Get current row as text string.
    /// </summary>
    /// <returns>Row text string.</returns>
    protected ReadOnlySequence<char> GetRowText() => _delimiterStreamReader.GetLine();

    /// <summary>
    /// Get column row string value.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>The string value.</returns>
    protected ReadOnlySpan<char> GetColumnValue(int columnIndex)
    {
        if (_cacheIterator != null)
        {
            return _cacheIterator.Current[columnIndex].AsString;
        }
        if (columnIndex < _virtualColumnsCount)
        {
            return GetVirtualColumnValue(_rowIndex, columnIndex).AsString;
        }
        columnIndex -= _virtualColumnsCount;

        return _delimiterStreamReader.GetField(columnIndex);
    }

    protected ReadOnlySpan<char> GetInputColumnValue(int columnIndex)
        => GetColumnValue(columnIndex + _virtualColumnsCount);

    /// <inheritdoc />
    public virtual void Open()
    {
        _logger.LogDebug("Start stream open.");
        _virtualColumnsCount = GetVirtualColumns().Length;
        var inputIterator = new RowsInputIterator(this, autoFetch: true);

        if (DetectColumnsTypes)
        {
            // Move iterator, after that we are able to fill initial columns set.
            var hasData = inputIterator.MoveNext();
            if (!hasData)
            {
                return;
            }

            // Prepare cache iterator. Analyze might read data which we cache and
            // then provide from memory instead from input source.
            var cacheIterator = new CacheRowsIterator(inputIterator);
            cacheIterator.AddRow(inputIterator.Current);
            cacheIterator.SeekToHead();

            Analyze(cacheIterator);
            cacheIterator.SeekToHead();
            cacheIterator.Freeze();
            _cacheIterator = cacheIterator;
        }

        _logger.LogDebug("Open stream finished.");
    }

    /// <summary>
    /// The method is called to analyze current dataset (get columns, resolve types, etc).
    /// The data that was read by iterator will be cached.
    /// </summary>
    /// <param name="iterator">Rows iterator.</param>
    protected abstract void Analyze(CacheRowsIterator iterator);

    /// <summary>
    /// The method should return custom (virtual) columns array.
    /// </summary>
    /// <returns>Virtual columns array.</returns>
    protected virtual Column[] GetVirtualColumns()
    {
        return _options.AddInputSourceColumn
               && (StreamReader.BaseStream is FileStream || StreamReader.BaseStream is GZipStream)
            ? _customColumns
            : [];
    }

    /// <summary>
    /// The method should get value for the specific custom column.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>Value.</returns>
    protected virtual VariantValue GetVirtualColumnValue(int rowIndex, int columnIndex)
    {
        if (columnIndex == 0 && StreamReader.BaseStream is FileStream fileStream)
        {
            return new VariantValue(fileStream.Name);
        }
        if (columnIndex == 0 && StreamReader.BaseStream is GZipStream zipStream
            && zipStream.BaseStream is FileStream zipFileStream)
        {
            return new VariantValue(zipFileStream.Name);
        }
        return VariantValue.Null;
    }

    /// <summary>
    /// Get input column (all columns excluding virtual).
    /// </summary>
    /// <returns>Input columns.</returns>
    protected Span<Column> GetInputColumns() => _columns.AsSpan(_virtualColumnsCount);

    /// <summary>
    /// Get input value (all values excluding virtual).
    /// </summary>
    /// <param name="row">Row to get values.</param>
    /// <returns>Input values.</returns>
    protected Span<VariantValue> GetCurrentInputValues(Row row)
    {
        var values = new VariantValue[_columns.Length - _virtualColumnsCount];
        for (var i = _virtualColumnsCount; i < _columns.Length; i++)
        {
            values[i - _virtualColumnsCount] = row[i];
        }
        return values;
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        _logger.LogDebug("Stream dispose.");
        if (disposing)
        {
            StreamReader.Dispose();
            _isClosed = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString()
    {
        if (StreamReader.BaseStream is FileStream fileStream)
        {
            return $"{nameof(StreamRowsInput)}: {fileStream.Name}";
        }
        return base.ToString() ?? string.Empty;
    }
}
