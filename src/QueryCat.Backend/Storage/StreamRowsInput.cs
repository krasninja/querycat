using System.Buffers;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Utils;

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
     * - OpenAsync()
     *   - InitializeColumnsAsync() - get initial columns.
     *   - InitializeHeadDataAsync() - detect header, modify initial read cache frame.
     *   - InitializeColumnsTypesAsync() - resolve types by columns.
     *   - InitializeCompleteAsync() - complete initialization, get final types.
     * - SetContext()
     * - ReadNextAsync() - real data reading, cache first, stream next.
     * - CloseAsync()
     */

    private readonly StreamRowsInputOptions _options;

    private static readonly VirtualColumn[] _customColumns =
    [
        new("filename", DataType.String, "File path.") // Index 0.
    ];

    private int _rowIndex;

    /// <summary>
    /// Row index.
    /// </summary>
    protected int RowIndex => _rowIndex;

    protected StreamReader StreamReader { get; }

    private readonly Stream _baseStream;
    private readonly CacheStream _cacheStream;

    private bool _isClosed;
    private bool _isOpened;

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

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
    /// <param name="stream">Stream to read.</param>
    /// <param name="options">The options.</param>
    /// <param name="keys">Unique keys.</param>
    protected StreamRowsInput(
        Stream stream,
        StreamRowsInputOptions? options = null,
        params IEnumerable<string> keys)
    {
        _options = options ?? new();
        _baseStream = stream;
        _cacheStream = new CacheStream(stream);
        StreamReader = new StreamReader(_cacheStream);
        _delimiterStreamReader = new DelimiterStreamReader(StreamReader, _options.DelimiterStreamReaderOptions);
        UniqueKey = keys.Concat(_options.CacheKeys).ToArray();
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
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Close {Stream}.", this);
        Dispose(true);
        return Task.CompletedTask;
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
    /// Initialize columns with the new set.
    /// </summary>
    /// <param name="newColumns">New columns.</param>
    private void SetColumns(IReadOnlyList<Column> newColumns)
    {
        if (newColumns.Count < 1)
        {
            throw new QueryCatException(Resources.Errors.NoColumns);
        }

        var virtualColumns = GetVirtualColumns();
        _virtualColumnsCount = virtualColumns.Length;
        var nonVirtualColumns = newColumns.Where(c => c is not VirtualColumn).ToArray();
        var newColumnsLength = nonVirtualColumns.Length + _virtualColumnsCount;
        Array.Resize(ref _columns, newColumnsLength);

        // Add virtual columns.
        for (var i = 0; i < _virtualColumnsCount; i++)
        {
            _columns[i] = new VirtualColumn(virtualColumns[i]);
        }

        // Add non-virtual columns.
        for (var i = _virtualColumnsCount; i < newColumnsLength; i++)
        {
            _columns[i] = nonVirtualColumns[i - _virtualColumnsCount];
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        bool hasData;
        do
        {
            hasData = false;

            // If we have cached data - return it first.
            if (_cacheIterator != null)
            {
                if (await _cacheIterator.MoveNextAsync(cancellationToken))
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
                hasData = await ReadNextInternalAsync(cancellationToken);
            }
            if (hasData)
            {
                _rowIndex++;
            }
        }
        while (hasData && IgnoreLine());
        return hasData;
    }

    /// <summary>
    /// Actual next data reading.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>True</c> if it has data, <c>false</c> otherwise.</returns>
    protected virtual ValueTask<bool> ReadNextInternalAsync(CancellationToken cancellationToken)
    {
        return _delimiterStreamReader.ReadAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Reset stream.");

        ResetCacheToInitialPosition();
        if (_cacheIterator == null)
        {
            _baseStream.Seek(0, SeekOrigin.Begin);
        }
        return Task.CompletedTask;
    }

    private void ResetCacheToInitialPosition()
    {
        // If we still read cache data - we just reset it. Otherwise, there will be double read.
        if (_cacheIterator != null)
        {
            _cacheIterator.SeekCacheCursorToHead();
        }
        else
        {
            StreamReader.DiscardBufferedData();
            _delimiterStreamReader.Reset();
            _cacheStream.Seek(0, SeekOrigin.Begin);
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
    private ReadOnlySpan<char> GetColumnValue(int columnIndex)
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

    /// <summary>
    /// Get column raw string value within the current row. Takes into account virtual columns.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>String value.</returns>
    protected ReadOnlySpan<char> GetInputColumnValue(int columnIndex)
        => GetColumnValue(columnIndex + _virtualColumnsCount);

    /// <summary>
    /// The method is called when we need to figure out the initial count of columns. On this stage
    /// only lines from the source are supported to read.
    /// </summary>
    /// <param name="input">Rows input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Column related to the source.</returns>
    protected virtual async Task<Column[]> InitializeColumnsAsync(
        IRowsInput input,
        CancellationToken cancellationToken = default)
    {
        await ReadNextAsync(cancellationToken);
        var fieldsCount = _delimiterStreamReader.GetFieldsCount();
        var newColumns = new Column[fieldsCount];
        for (var i = 0; i < newColumns.Length; i++)
        {
            newColumns[i] = new Column(i, DataType.String);
        }
        return newColumns.ToArray();
    }

    /// <summary>
    /// The method is called when the schema is ready. On this stage you can modify initial
    /// data. It can be useful if you want to update schema and want to use first row as header (it should not
    /// be a part of data).
    /// </summary>
    /// <param name="iterator">Cache iterator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task InitializeHeadDataAsync(CacheRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// The method is called to detect columns types.
    /// </summary>
    /// <param name="iterator">Data iterator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual async Task InitializeColumnsTypesAsync(IRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        if (DetectColumnsTypes)
        {
            await RowsIteratorUtils.ResolveColumnsTypesAsync(iterator, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// The method is called when initial read frame is initialized and schema is prepared.
    /// </summary>
    /// <param name="iterator">Rows iterator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task InitializeCompleteAsync(CacheRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_isOpened)
        {
            return;
        }

        _logger.LogDebug("Start stream open.");
        _virtualColumnsCount = GetVirtualColumns().Length;
        var inputIterator = new RowsInputIterator(this, autoFetch: true);
        var cacheIterator = new CacheRowsIterator(inputIterator);

        // Initialize columns.
        var columns = await InitializeColumnsAsync(this, cancellationToken);
        SetColumns(columns);
        ResetCacheToInitialPosition();
        _cacheStream.Freeze();

        // Initialize head data frame.
        await InitializeHeadDataAsync(cacheIterator, cancellationToken);
        cacheIterator.SeekCacheCursorToHead();

        // Types detection, or they would be strings by default.
        await InitializeColumnsTypesAsync(cacheIterator, cancellationToken);
        cacheIterator.SeekCacheCursorToHead();

        // Prepare cache iterator. Analyze might read data which we cache and
        // then provide it from memory instead from input source.
        await InitializeCompleteAsync(cacheIterator, cancellationToken);
        cacheIterator.SeekCacheCursorToHead();

        _cacheIterator = cacheIterator;
        _cacheIterator.Freeze();

        _logger.LogDebug("Open stream finished.");
        _isOpened = true;
    }

    /// <summary>
    /// The method should return custom (virtual) columns array.
    /// </summary>
    /// <returns>Virtual columns array.</returns>
    protected virtual VirtualColumn[] GetVirtualColumns()
    {
        return _options.AddInputSourceColumn && (_baseStream is FileStream || _baseStream is GZipStream)
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
        if (columnIndex == 0 && _baseStream is FileStream fileStream)
        {
            return new VariantValue(fileStream.Name);
        }
        if (columnIndex == 0 && _baseStream is GZipStream zipStream
            && _baseStream is FileStream zipFileStream)
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
            _baseStream.Dispose();
            _cacheStream.Dispose();
            _isClosed = true;
            _isOpened = false;
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
        if (_baseStream is FileStream fileStream)
        {
            return $"{nameof(StreamRowsInput)}: {fileStream.Name}";
        }
        return base.ToString() ?? string.Empty;
    }
}
