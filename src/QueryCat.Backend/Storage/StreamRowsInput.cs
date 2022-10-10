using System.Buffers;
using System.Runtime.CompilerServices;
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

    protected DynamicBuffer<char> DynamicBuffer { get; }

    protected abstract StreamReader? StreamReader { get; set; }

    protected StreamRowsInputOptions Options { get; }

    public QueryContext QueryContext { get; private set; } = EmptyQueryContext.Empty;

    private readonly char[] _stopCharacters;

    // Stores positions of delimiters for columns.
    private int[] _currentDelimitersPositions = new int[32];
    private bool[] _currentDelimitersQuotes = new bool[32];
    private int _currentDelimitersPositionsCursor;

    // Little optimization to prevent unnecessary DynamicBuffer.GetSequence() calls.
    private ReadOnlySequence<char> _currentSequence = ReadOnlySequence<char>.Empty;

    // Current position in a reading row. We need it in a case if read current row and need to
    // fetch new data to finish. The current position will contain the start index.
    private int _currentDelimiterPosition;
    private bool _firstFetch = true;

    // Cache.
    private RowsFrameIterator? _preReadIterator;

    /// <inheritdoc />
    public Column[] Columns { get; protected set; } = Array.Empty<Column>();

    private int _customColumnsCount = 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">The options.</param>
    protected StreamRowsInput(StreamRowsInputOptions? options = null)
    {
        Options = options ?? new StreamRowsInputOptions();
        DynamicBuffer = new DynamicBuffer<char>(Options.BufferSize);

        // Stop characters.
        var stopCharactersList = new List<char>(10);
        stopCharactersList.AddRange(Options.Delimiters);
        if (Options.UseQuoteChar)
        {
            stopCharactersList.Add(Options.QuoteChar);
        }
        stopCharactersList.AddRange(new[] { '\n', '\r' });
        _stopCharacters = stopCharactersList.ToArray();

        _currentDelimitersPositions[0] = 0;
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

        Columns = new Column[_currentDelimitersPositionsCursor + _customColumnsCount - 1];
        for (var i = 0; i < _customColumnsCount; i++)
        {
            Columns[i] = new Column(customColumns[i]);
        }
        for (var i = _customColumnsCount; i < Columns.Length; i++)
        {
            Columns[i] = new Column(i + 1 - _customColumnsCount, DataType.String);
        }
    }

    /// <inheritdoc />
    public bool ReadNext()
    {
        bool hasData;
        do
        {
            hasData = ReadNextWithDelimiters(Options.Delimiters);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char GetNextCharacter()
    {
        if (DynamicBuffer.Size <= _currentDelimiterPosition)
        {
            ReadNextBufferData();
        }
        if (DynamicBuffer.Size > _currentDelimiterPosition)
        {
            return DynamicBuffer.GetAt(_currentDelimiterPosition);
        }
        return '\0';
    }

    /// <summary>
    /// Read the next row using custom delimiters.
    /// </summary>
    /// <param name="delimiters">Custom delimiters.</param>
    /// <returns><c>True</c> if cursor was moved and data is available, <c>false</c> if there is no row anymore.</returns>
    protected bool ReadNextWithDelimiters(char[]? delimiters = null)
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

        _currentDelimitersPositionsCursor = 1;
        DynamicBuffer.Advance(_currentDelimiterPosition);
        _currentDelimiterPosition = 0;
        if (DynamicBuffer.IsEmpty)
        {
            ReadNextBufferData();
        }

        int readBytes;
        bool isInQuotes = false, // Are we within quotes?
            quotesMode = false; // Set when first field char is quote.
        SequenceReader<char> sequenceReader;
        do
        {
            _currentSequence = DynamicBuffer.GetSequence();
            sequenceReader = new SequenceReader<char>(_currentSequence);
            sequenceReader.Advance(_currentDelimiterPosition);
            while (_currentDelimiterPosition < DynamicBuffer.Size)
            {
                var hasAdvanced = !isInQuotes
                    ? sequenceReader.TryAdvanceToAny(_stopCharacters, advancePastDelimiter: false)
                    : sequenceReader.TryAdvanceTo(Options.QuoteChar, advancePastDelimiter: false);
                if (!hasAdvanced)
                {
                    _currentDelimiterPosition = (int)sequenceReader.Consumed;
                    break;
                }

                sequenceReader.TryPeek(out char ch);
                sequenceReader.Advance(1);
                if (Options.UseQuoteChar && ch == Options.QuoteChar)
                {
                    // If the field starts with quote - turn on the quotes processing mode.
                    if (_currentDelimitersPositions[_currentDelimitersPositionsCursor - 1] == (int)sequenceReader.Consumed - 1)
                    {
                        quotesMode = true;
                    }
                    if (quotesMode)
                    {
                        isInQuotes = !isInQuotes;
                    }
                }
                else if (delimiters != null && Array.IndexOf(delimiters, ch) > -1)
                {
                    _currentDelimiterPosition = (int)sequenceReader.Consumed;
                    if (!isInQuotes)
                    {
                        AddDelimiterPosition(_currentDelimiterPosition, quotesMode);
                        quotesMode = false;
                    }
                }
                else if (ch == '\n' || ch == '\r')
                {
                    if (!isInQuotes)
                    {
                        _currentDelimiterPosition = (int)sequenceReader.Consumed;
                        AddDelimiterPosition(_currentDelimiterPosition, quotesMode);
                        quotesMode = false;

                        // Process /r/n Windows line end case.
                        if (ch == '\r' && GetNextCharacter() == '\n')
                        {
                            _currentDelimiterPosition++;
                        }

                        // Skip empty line and try to read next.
                        if (Options.SkipEmptyLines && IsEmpty())
                        {
                            sequenceReader.Advance(_currentDelimiterPosition);
                            _currentDelimitersPositionsCursor = 1;
                            continue;
                        }

                        return true;
                    }

                    _currentDelimiterPosition = (int)sequenceReader.Consumed;
                    break;
                }
            }

            readBytes = ReadNextBufferData();
        }
        while (readBytes > 0);

        // We are at the end of the stream. Add remain index and exit.
        _currentDelimiterPosition += (int)sequenceReader.Remaining;
        AddDelimiterPosition(_currentDelimiterPosition + 1, quotesMode);
        return !IsEmpty();
    }

    private bool IsEmpty()
        => _currentDelimitersPositionsCursor == 2 && _currentDelimitersPositions[1] == 1;

    private int ReadNextBufferData()
    {
        if (StreamReader!.EndOfStream)
        {
            return 0;
        }

        var buffer = DynamicBuffer.Allocate();
        var readBytes = StreamReader.Read(buffer);
        DynamicBuffer.Commit(readBytes);
        return readBytes;
    }

    private void AddDelimiterPosition(int value, bool quotesMode)
    {
        _currentDelimitersPositionsCursor++;
        if (_currentDelimitersPositions.Length < _currentDelimitersPositionsCursor)
        {
            Array.Resize(ref _currentDelimitersPositions, _currentDelimitersPositionsCursor);
            Array.Resize(ref _currentDelimitersQuotes, _currentDelimitersPositionsCursor);
        }
        _currentDelimitersPositions[_currentDelimitersPositionsCursor - 1] = value;
        _currentDelimitersQuotes[_currentDelimitersPositionsCursor - 1] = quotesMode;
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
    protected ReadOnlySequence<char> GetRowText()
    {
        if (_currentDelimitersPositionsCursor < 1)
        {
            return ReadOnlySequence<char>.Empty;
        }
        return _currentSequence.Slice(
            _currentDelimitersPositions[0], _currentDelimitersPositions[_currentDelimitersPositionsCursor - 1] - 1);
    }

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
        if (columnIndex + 1 > _currentDelimitersPositionsCursor - 1)
        {
            return ReadOnlySpan<char>.Empty;
        }

        var previousColumnIndex = _currentDelimitersPositions[columnIndex];
        var currentColumnIndex = _currentDelimitersPositions[columnIndex + 1];
        if (previousColumnIndex == currentColumnIndex)
        {
            return ReadOnlySpan<char>.Empty;
        }
        var valueSequence = _currentSequence.Slice(previousColumnIndex, currentColumnIndex - previousColumnIndex - 1);
        var valueHasQuotes = Options.UseQuoteChar && _currentDelimitersQuotes[columnIndex + 1];
        if (valueHasQuotes)
        {
            return StringUtils.Unquote(valueSequence);
        }
        return valueSequence.IsSingleSegment ? valueSequence.FirstSpan : valueSequence.ToArray();
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

    /// <inheritdoc />
    public override string ToString()
    {
        if (StreamReader?.BaseStream is FileStream fileStream)
        {
            return Path.GetFileName(fileStream.Name).Trim();
        }
        return GetType().Name;
    }
}
