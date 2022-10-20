using System.Buffers;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Types;
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
    private readonly StreamRowsInputOptions? _options;

    /*
     * The main workflow is:
     * - Open()
     *   - Analyze() - init columns, add custom columns, resolve columns types
     * - SetContext()
     * - ReadNext() - real data reading, cache first, stream next
     * - Close()
     */
    private int _rowIndex = 0;

    protected StreamReader StreamReader { get; }

    private bool _isClosed;

    public QueryContext QueryContext { get; private set; } = EmptyQueryContext.Empty;

    // Current position in a reading row. We need it in a case if read current row and need to
    // fetch new data to finish. The current position will contain the start index.
    private bool _firstFetch = true;

    // Cache.
    private CacheRowsIterator? _cacheIterator;

    private Column[] _columns = { };

    /// <inheritdoc />
    public Column[] Columns => _columns;

    private int _virtualColumnsCount = 0;

    private readonly DelimiterStreamReader _delimiterStreamReader;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="streamReader">Stream reader.</param>
    /// <param name="options">The options.</param>
    public StreamRowsInput(StreamReader streamReader, StreamRowsInputOptions? options = null)
    {
        _options = options ?? new();
        _delimiterStreamReader = new DelimiterStreamReader(streamReader, _options.DelimiterStreamReaderOptions);
        StreamReader = streamReader;
    }

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
        QueryContext = queryContext;
    }

    /// <inheritdoc />
    public void Close()
    {
        Logger.Instance.Debug("Close.", nameof(StreamRowsInput));
        Dispose(true);
    }

    /// <inheritdoc />
    public virtual ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        var columnValue = GetColumnValue(columnIndex);
        return VariantValue.TryCreateFromString(
            columnValue,
            Columns[columnIndex].DataType,
            out value)
            ? ErrorCode.OK : ErrorCode.CannotCast;
    }

    private void SetDefaultColumns()
    {
        var virtualColumns = GetVirtualColumns();
        _virtualColumnsCount = virtualColumns.Length;

        var newColumns = new Column[_delimiterStreamReader.GetFieldsCount() + _virtualColumnsCount];
        for (var i = 0; i < _virtualColumnsCount; i++)
        {
            newColumns[i] = new Column(virtualColumns[i]);
        }
        for (var i = _virtualColumnsCount; i < newColumns.Length; i++)
        {
            newColumns[i] = new Column(i - _virtualColumnsCount, DataType.String);
        }
        SetColumns(newColumns);
    }

    /// <summary>
    /// Initialize columns with the new set.
    /// </summary>
    /// <param name="columns">New columns.</param>
    protected void SetColumns(IEnumerable<Column> columns)
    {
        var newColumns = columns as Column[] ?? columns.ToArray();
        Array.Resize(ref _columns, newColumns.Length);
        Array.Copy(newColumns, _columns, newColumns.Length);
    }

    /// <inheritdoc />
    public virtual bool ReadNext()
    {
        bool hasData;
        do
        {
            hasData = ReadNextInternal();
            if (hasData && _firstFetch)
            {
                SetDefaultColumns();
                _firstFetch = false;
            }
        }
        while (hasData && IgnoreLine());
        return hasData;
    }

    private bool ReadNextInternal()
    {
        _rowIndex++;

        // If we have cached data - return it first.
        if (_cacheIterator != null)
        {
            if (_cacheIterator.MoveNext())
            {
                return true;
            }
            _cacheIterator = null;
        }

        if (_isClosed)
        {
            return false;
        }

        return _delimiterStreamReader.Read();
    }

    /// <inheritdoc />
    public virtual void Reset()
    {
        Logger.Instance.Debug("Reset.", nameof(StreamRowsInput));
        StreamReader.DiscardBufferedData();
        StreamReader.BaseStream.Seek(0, SeekOrigin.Begin);
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

    /// <inheritdoc />
    public virtual void Open()
    {
        var inputIterator = new RowsInputIterator(this, autoFetch: true);

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
        cacheIterator.Seek(-1, CursorSeekOrigin.Begin);

        var startRowIndex = Analyze(cacheIterator);
        cacheIterator.Seek(startRowIndex, CursorSeekOrigin.Begin);
        cacheIterator.Freeze();
        _cacheIterator = cacheIterator;
    }

    /// <summary>
    /// The method is called to analyze current dataset (get columns, resolve types, etc).
    /// The data that was read by iterator will be cached.
    /// </summary>
    /// <param name="iterator">Rows iterator.</param>
    /// <returns>The row index where the data starts.</returns>
    protected virtual int Analyze(ICursorRowsIterator iterator)
    {
        return -1;
    }

    /// <summary>
    /// The method should return custom (virtual) columns array.
    /// </summary>
    /// <returns>Virtual columns array.</returns>
    protected virtual Column[] GetVirtualColumns() => Array.Empty<Column>();

    /// <summary>
    /// The method should get value for the specific custom column.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>Value.</returns>
    protected virtual VariantValue GetVirtualColumnValue(int rowIndex, int columnIndex) => VariantValue.Null;

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
        for (int i = _virtualColumnsCount; i < _columns.Length; i++)
        {
            values[i - _virtualColumnsCount] = row[i];
        }
        return values;
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
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
}
