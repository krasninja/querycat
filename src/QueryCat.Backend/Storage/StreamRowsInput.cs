using System.Buffers;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The base class that contains common logic to parse text data from a stream. It
/// stores the delimiter indexes within the buffer.
/// </summary>
public abstract class StreamRowsInput : IRowsInput, IDisposable
{
    /*
     * The main workflow is:
     * - Open()
     *   - Analyze() - init columns, add custom columns, resolve columns types
     *     - ReadNext() - store data into RowsFrame, on first call init columns
     *   - Prepare() - prepare for reading
     * - SetContext()
     * - ReadNext() - real data reading, cache first, stream next
     * - Close()
     */
    private int _rowIndex = 0;

    protected abstract StreamReader? StreamReader { get; set; }

    public QueryContext QueryContext { get; private set; } = EmptyQueryContext.Empty;

    // Current position in a reading row. We need it in a case if read current row and need to
    // fetch new data to finish. The current position will contain the start index.
    private bool _firstFetch = true;

    // Cache.
    private RowsFrameIterator? _preReadIterator;

    /// <inheritdoc />
    public Column[] Columns { get; protected set; } = Array.Empty<Column>();

    private int _customColumnsCount = 0;

    private readonly DelimiterStreamReader _delimiterStreamReader;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="streamReader">Stream reader.</param>
    /// <param name="options">The options.</param>
    public StreamRowsInput(StreamReader streamReader, DelimiterStreamReader.ReaderOptions? options = null)
    {
        _delimiterStreamReader = new DelimiterStreamReader(streamReader, options);
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
        return VariantValue.TryCreateFromString(
            GetColumnValue(columnIndex),
            Columns[columnIndex].DataType,
            out value)
            ? ErrorCode.OK : ErrorCode.CannotCast;
    }

    private void SetDefaultColumns()
    {
        var customColumns = GetCustomColumns();
        _customColumnsCount = customColumns.Length;

        Columns = new Column[_delimiterStreamReader.GetFieldsCount() + _customColumnsCount];
        for (var i = 0; i < _customColumnsCount; i++)
        {
            Columns[i] = new Column(customColumns[i]);
        }
        for (var i = _customColumnsCount; i < Columns.Length; i++)
        {
            Columns[i] = new Column(i - _customColumnsCount, DataType.String);
        }
    }

    /// <inheritdoc />
    public bool ReadNext()
    {
        bool hasData;
        do
        {
            hasData = ReadNextWithDelimiters();
            if (hasData && _firstFetch)
            {
                SetDefaultColumns();
                _firstFetch = false;
            }
        }
        while (hasData && IgnoreLine());
        return hasData;
    }

    /// <inheritdoc />
    public virtual void Reset()
    {
        if (StreamReader != null)
        {
            Logger.Instance.Debug("Reset.", nameof(StreamRowsInput));
            StreamReader.DiscardBufferedData();
            StreamReader.BaseStream.Seek(0, SeekOrigin.Begin);
        }
    }

    /// <summary>
    /// Read the next row using custom delimiters.
    /// </summary>
    /// <returns><c>True</c> if cursor was moved and data is available, <c>false</c> if there is no row anymore.</returns>
    protected bool ReadNextWithDelimiters()
    {
        _rowIndex++;

        // If we have cached data - return it first.
        if (_preReadIterator != null && _preReadIterator.MoveNext())
        {
            return true;
        }

        if (StreamReader == null)
        {
            return false;
        }

        return _delimiterStreamReader.Read();
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
        if (_preReadIterator != null && _preReadIterator.HasData)
        {
            return _preReadIterator.Current[columnIndex].AsString;
        }
        if (columnIndex < _customColumnsCount)
        {
            return GetCustomColumnValue(_rowIndex, columnIndex).AsString;
        }
        columnIndex -= _customColumnsCount;

        return _delimiterStreamReader.GetField(columnIndex);
    }

    /// <inheritdoc />
    public virtual void Open()
    {
        // input iterator -> action to append cache.
        var inputIterator = new RowsInputIterator(this, autoFetch: true);
        RowsFrameIterator? localPreReadIterator = null;
        var actionRowsIterator = new ActionRowsIterator(inputIterator, "add to pre-read cache")
        {
            AfterMoveNext = afterMoveIterator =>
            {
                if (localPreReadIterator == null)
                {
                    var preReadRowsFrame = new RowsFrame(Columns);
                    localPreReadIterator = preReadRowsFrame.GetIterator();
                }
                localPreReadIterator.RowsFrame.AddRow(afterMoveIterator.Current);
            }
        };
        Analyze(actionRowsIterator);
        if (localPreReadIterator != null)
        {
            localPreReadIterator.Reset();
            _preReadIterator = localPreReadIterator;
            Prepare(localPreReadIterator);
        }
    }

    /// <summary>
    /// The method is called to analyze current dataset (get columns, resolve types, etc).
    /// The data that was read by iterator will be cached.
    /// </summary>
    /// <param name="iterator">Rows iterator.</param>
    protected virtual void Analyze(IRowsIterator iterator)
    {
    }

    /// <summary>
    /// Prepare for data set reading. The method is called right after data analysis and before
    /// actual data read.
    /// </summary>
    /// <param name="iterator">Rows iterator.</param>
    protected virtual void Prepare(IRowsIterator iterator)
    {
    }

    protected virtual Column[] GetCustomColumns() => Array.Empty<Column>();

    protected virtual VariantValue GetCustomColumnValue(int rowIndex, int columnIndex) => VariantValue.Null;

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StreamReader?.Dispose();
            StreamReader = null;
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
