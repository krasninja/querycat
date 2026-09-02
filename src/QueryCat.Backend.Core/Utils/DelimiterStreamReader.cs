using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Simple text parser with the delimiters. Can be used to parse CSV, DSV, stdin stream, etc.
/// </summary>
public class DelimiterStreamReader
{
    /*
     * The basic use case:
     *
     * var csv = new DelimiterStreamReader(new StreamReader(file));
     * await csv.ReadAsync(); // Read header.
     * while (await csv.ReadAsync())
     * {
     *     csv.GetInt32(0); // Read column #0 as int.
     *     csv.GetField(1); // Read column #1 as string.
     * }
     */
    private const int DefaultBufferSize = 0x5000; // ~20 KB

    private const int StateRoute = 0;
    private const int StateReadField = 1;
    private const int StateReadQuoteField = 2;
    private const int StateAdvanceDelimiter = 10;
    private const int StateAdvanceNewLine1 = 11;
    private const int StateAdvanceNewLine2 = 12;

    private static readonly char[] _autoDetectDelimiters = [',', '\t', ';', '|'];
    private static readonly char[] _endOfLineCharacters = ['\n', '\r'];
    private static readonly SearchValues<char> _endOfLineCharactersSearch = SearchValues.Create(_endOfLineCharacters);

    private static readonly SimpleObjectPool<StringBuilder> _stringBuilderPool = new(() => new StringBuilder(), sb => sb.Clear());

    public delegate void OnDelimiterDelegate(char ch, long position, out bool countField, out bool endLine);

    /// <summary>
    /// On delimiter found delegate.
    /// </summary>
    public OnDelimiterDelegate? OnDelimiter { get; set; }

    /// <summary>
    /// Use async read.
    /// </summary>
    public bool AsyncRead { get; set; } = OperatingSystem.IsBrowser();

    /// <summary>
    /// Current line index.
    /// </summary>
    public long LineIndex { get; private set; } = -1;

    /// <summary>
    /// Quotes escape mode.
    /// </summary>
    public enum QuotesMode
    {
        /// <summary>
        /// Double quotes like "".
        /// </summary>
        DoubleQuotes,

        /// <summary>
        /// Backslash usage like \".
        /// </summary>
        Backslash
    }

    /// <summary>
    /// Options for <see cref="DelimiterStreamReader" />.
    /// </summary>
    public sealed class ReaderOptions
    {
        /// <summary>
        /// Quote character.
        /// </summary>
        public char[] QuoteChars { get; set; } = [];

        /// <summary>
        /// Quote mode will be enabled only if quote is in the beginning of field.
        /// </summary>
        /// <example>
        /// true: "one two ""three""" -> one two "three";
        /// true: one two ""three"" -> one two ""three"";
        /// false: "one two ""three""" -> one two "three";
        /// true: one two ""three"" -> one two "three".
        /// </example>
        public bool EnableQuotesModeOnFieldStart { get; set; } = true;

        /// <summary>
        /// Columns delimiters.
        /// </summary>
        public char[] Delimiters { get; set; } = [];

        /// <summary>
        /// Attempt to find delimiter if it is not specified. If not found the PreferredDelimiter
        /// will be used (if set).
        /// </summary>
        public bool DetectDelimiter { get; set; } = true;

        /// <summary>
        /// Get the preferred delimiter if we cannot determine any from line.
        /// </summary>
        public char? PreferredDelimiter { get; set; }

        /// <summary>
        /// Skip extra delimiters. Can be useful for example for whitespace delimiter.
        /// </summary>
        public bool SkipRepeatedDelimiters { get; set; }

        /// <summary>
        /// Buffer size.
        /// </summary>
        public int BufferSize { get; set; } = DefaultBufferSize;

        /// <summary>
        /// Do not take into account empty lines.
        /// </summary>
        public bool SkipEmptyLines { get; set; } = true;

        /// <summary>
        /// Culture to use for parse.
        /// </summary>
        public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

        /// <summary>
        /// Complete row reading if reach end of line characters.
        /// </summary>
        public bool CompleteOnEndOfLine { get; set; } = true;

        /// <summary>
        /// Quotes escape style.
        /// </summary>
        public QuotesMode QuotesEscapeStyle { get; set; } = QuotesMode.DoubleQuotes;

        /// <summary>
        /// Include delimiter into field result. False by default.
        /// </summary>
        public bool IncludeDelimiter { get; set; }
    }

    private sealed class FieldInfo
    {
        public DynamicBuffer<char>.DynamicBufferPosition Start { get; set; }

        public int Length { get; set; } = -1;

        public ulong QuotesCount { get; set; }

        public char QuoteCharacter { get; set; }

        public long EndQuotePosition { get; set; }

        public bool HasQuotes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return QuotesCount > 0; }
        }

        public bool HasInnerQuotes => QuotesCount > 2;

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)Length == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Length = 0;
            QuotesCount = 0;
            EndQuotePosition = 0;
        }
    }

    private readonly DynamicBuffer<char> _dynamicBuffer;
    private readonly StreamReader _streamReader;
    private readonly ReaderOptions _options;
    private char[] _stopCharacters = [];
    private SearchValues<char> _stopCharactersSearch = SearchValues.Create(ReadOnlySpan<char>.Empty);
    private SearchValues<char> _delimitersSearch = SearchValues.Create(ReadOnlySpan<char>.Empty);
    private SearchValues<char> _delimitersEndOfLineSearch = SearchValues.Create(ReadOnlySpan<char>.Empty);
    private SearchValues<char> _quoteCharactersSearch = SearchValues.Create(ReadOnlySpan<char>.Empty);
    private int _parseState = StateReadField;
    private bool _noData;

    // Stores positions of delimiters for columns.
    private FieldInfo[] _fieldInfos;
    private int _fieldInfoLastIndex;

    // Current position in a reading row. We need it in a case if read current row and need to
    // fetch new data to finish. The current position will contain the start index.
    private long _currentDelimiterPosition;
    private readonly DynamicBuffer<char>.DynamicBufferReader _bufferReader;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="streamReader">Stream reader.</param>
    /// <param name="options">Reader options.</param>
    public DelimiterStreamReader(StreamReader streamReader, ReaderOptions? options = null)
    {
        _streamReader = streamReader;
        _options = options ?? new ReaderOptions
        {
            Culture = Application.Culture,
        };
        _dynamicBuffer = new DynamicBuffer<char>(_options.BufferSize);
        _bufferReader = new DynamicBuffer<char>.DynamicBufferReader(_dynamicBuffer);

        _fieldInfos = new FieldInfo[32];
        for (var i = 0; i < _fieldInfos.Length; i++)
        {
            _fieldInfos[i] = new FieldInfo();
        }
        InitStopCharacters();
    }

    private void InitStopCharacters()
    {
        var endOfLineCharacters = _options.CompleteOnEndOfLine ? _endOfLineCharacters : [];
        _stopCharacters = _options.Delimiters
            .Union(endOfLineCharacters)
            .Union(_options.QuoteChars)
            .Distinct()
            .ToArray();
        _stopCharactersSearch = SearchValues.Create(_stopCharacters);
        _delimitersSearch = SearchValues.Create(_options.Delimiters);
        _delimitersEndOfLineSearch = SearchValues.Create(
            _options.Delimiters
            .Union(endOfLineCharacters)
            .Distinct()
            .ToArray()
        );
        _quoteCharactersSearch = SearchValues.Create(_options.QuoteChars);
    }

    /// <summary>
    /// Get current line fields count.
    /// </summary>
    /// <returns>Fields count.</returns>
    public int GetFieldsCount() => _fieldInfoLastIndex;

    /// <summary>
    /// Read the line ignoring delimiters and quotes.
    /// </summary>
    /// <returns><c>True</c> if the next data is available, <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> ReadLineAsync(CancellationToken cancellationToken = default)
        => ReadFields(lineMode: true, cancellationToken: cancellationToken);

    /// <summary>
    /// Read the line.
    /// </summary>
    /// <returns><c>True</c> if the next data is available, <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> ReadAsync(CancellationToken cancellationToken = default)
        => ReadFields(lineMode: false, cancellationToken: cancellationToken);

    private async ValueTask<bool> ReadFields(bool lineMode = false, CancellationToken cancellationToken = default)
    {
        _dynamicBuffer.Advance(_currentDelimiterPosition);
        _currentDelimiterPosition = 0;
        _fieldInfoLastIndex = 0;
        LineIndex++;

        if (_dynamicBuffer.IsEmpty || _noData)
        {
            var bytesRead = await ReadNextBufferDataAsync(cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead < 1 && _noData)
            {
                return false;
            }
            _noData = false;
        }
        if (_options.Delimiters.Length == 0 && _options.DetectDelimiter)
        {
            await FindDelimiterAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        _bufferReader.Reset();

        var ch = '\0';
        var currentField = (FieldInfo?)null;
        var fieldStartOffset = 0L;
        var fieldStartPosition = DynamicBuffer<char>.DynamicBufferPosition.Null;
        var quoteChar = '\0';
        var quotesCount = 0UL;
        var endQuotePosition = 0L;
        var completeLine = false;
        var stopCharactersSearch = lineMode ? _endOfLineCharactersSearch : _stopCharactersSearch;

        void AddField()
        {
            var addField = true;
            if (OnDelimiter != null && _delimitersSearch.Contains(ch))
            {
                OnDelimiter.Invoke(ch, _bufferReader.Consumed, out addField, out completeLine);
            }
            if (addField)
            {
                currentField = MoveToNextFieldInfo();
                if (!_options.IncludeDelimiter)
                {
                    currentField.Start = fieldStartPosition;
                    currentField.Length = (int)(_bufferReader.Consumed - fieldStartOffset);
                }
                else
                {
                    currentField.Start = _dynamicBuffer.GetPosition(0, fieldStartPosition);
                    currentField.Length = (int)(_bufferReader.Consumed - fieldStartOffset) + 1;
                }
                currentField.QuoteCharacter = quoteChar;
                currentField.QuotesCount = quotesCount;
                currentField.EndQuotePosition = endQuotePosition;
                quotesCount = 0;
                quoteChar = '\0';
                endQuotePosition = 0;
#if DEBUG
                if (currentField.Length > -1)
                {
                    var value = _dynamicBuffer.Slice(currentField.Start, currentField.Length);
                    Trace("found field: " + value.ToString());
                }
#endif
            }
        }

        Trace("start line");
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (true)
            {
                // StateReadField
                if (_parseState == StateReadField)
                {
                    if (_options.SkipRepeatedDelimiters)
                    {
                        _bufferReader.AdvancePastAny(_delimitersSearch);
                    }
                    if (_options.SkipEmptyLines && _fieldInfoLastIndex < 1)
                    {
                        _bufferReader.AdvancePastAny(_endOfLineCharactersSearch);
                    }
                    fieldStartOffset = _bufferReader.Consumed;
                    fieldStartPosition = _bufferReader.Position;
                    // Advance to any stop character (delimiter, quote, end of line).
                    if (_bufferReader.TryAdvanceToAny(stopCharactersSearch, advancePastDelimiter: false))
                    {
                        _parseState = StateRoute;
                        Trace("state change");
                    }
                    else
                    {
                        Trace("no data break");
                        break;
                    }
                }

                // StateRoute
                if (_parseState == StateRoute)
                {
                    ch = _bufferReader.Current;
                    if (!lineMode && _delimitersSearch.Contains(ch))
                    {
                        AddField();
                        _parseState = StateAdvanceDelimiter;
                        Trace("state change");
                    }
                    else if (_quoteCharactersSearch.Contains(ch))
                    {
                        // Case: line has quote inside, but starts without it
                        // > A "B"
                        if (_options.EnableQuotesModeOnFieldStart
                            && fieldStartOffset != _bufferReader.Consumed
                            && !HasOnlyWhitespaces(fieldStartPosition, _bufferReader.Consumed - fieldStartOffset))
                        {
                            // Try to find next delimiter or end of line.
                            if (_bufferReader.TryAdvanceToAny(_delimitersEndOfLineSearch, advancePastDelimiter: false))
                            {
                                _parseState = StateRoute;
                                continue;
                            }
                            _parseState = StateReadField;
                            _bufferReader.Seek(fieldStartPosition);
                            Trace("no data break");
                            break;
                        }
                        _parseState = StateReadQuoteField;
                        Trace("state change");
                    }
                    else if (_endOfLineCharactersSearch.Contains(ch) || _noData)
                    {
                        _currentDelimiterPosition = _bufferReader.Consumed;
                        _parseState = StateAdvanceNewLine1;
                        Trace("state change");
                        AddField();
                        return true;
                    }

                    if (completeLine)
                    {
                        _currentDelimiterPosition = _bufferReader.Consumed;
                        if (_bufferReader.Advance(1) < 1)
                        {
                            Trace("no data break");
                            break;
                        }
                        return true;
                    }
                }

                // StateAdvanceDelimiter
                if (_parseState == StateAdvanceDelimiter)
                {
                    if (_bufferReader.Advance(1) < 1)
                    {
                        Trace("no data break");
                        break;
                    }
                    _parseState = StateReadField;
                    Trace("state change");
                    continue;
                }

                // StateReadQuoteField
                if (_parseState == StateReadQuoteField)
                {
                    fieldStartOffset = _bufferReader.Consumed;
                    fieldStartPosition = _bufferReader.Position;
                    quoteChar = ch;
                    if (!ReadQuoteField(quoteChar, out quotesCount, out endQuotePosition))
                    {
                        _bufferReader.Seek(fieldStartPosition);
                        Trace("no data break");
                        break;
                    }
                    _parseState = StateRoute;
                    Trace("state change");
                    continue;
                }

                // StateAdvanceNewLine1
                if (_parseState == StateAdvanceNewLine1)
                {
                    if (_bufferReader.Advance(1) < 1)
                    {
                        Trace("no data break");
                        break;
                    }
                    _parseState = _endOfLineCharactersSearch.Contains(_bufferReader.Current)
                        ? StateAdvanceNewLine2
                        : StateReadField;
                    Trace("state change");
                }

                // StateAdvanceNewLine2
                if (_parseState == StateAdvanceNewLine2)
                {
                    if (_bufferReader.Advance(1) < 1)
                    {
                        Trace("no data break");
                        break;
                    }
                    _parseState = StateReadField;
                    Trace("state change");
                }
            }

            var readBytes = await ReadNextBufferDataAsync(cancellationToken)
                .ConfigureAwait(false);
            if (readBytes < 1)
            {
                _noData = true;
                if (_parseState == StateReadField || _parseState == StateReadQuoteField
                                                  || _parseState == StateAdvanceDelimiter)
                {
                    _bufferReader.AdvanceToEnd();
                    // With completeLine=true we've already added the field.
                    if (!completeLine)
                    {
                        ch = '\0';
                        AddField();
                    }
                    _parseState = StateAdvanceNewLine1;
                    Trace("state change");

                    _currentDelimiterPosition = _bufferReader.Consumed;
                    if (currentField != null && currentField.QuotesCount % 2 == 1)
                    {
                        currentField.EndQuotePosition = _bufferReader.Consumed;
                    }
                }
                return !NoFields();
            }
        }
    }

    [Conditional("DEBUG")]
    private static void Trace(string message)
    {
        System.Diagnostics.Trace.WriteLine(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasOnlyWhitespaces(DynamicBuffer<char>.DynamicBufferPosition startPosition, long length)
        => _dynamicBuffer.Slice(startPosition, length).IsWhiteSpace();

    private bool ReadQuoteField(char quoteChar, out ulong quotesCount, out long endQuotePosition)
    {
        quotesCount = 1;
        endQuotePosition = 0;

        // We are on the quote, pass it.
        if (_bufferReader.Advance(1) < 1)
        {
            return false;
        }
        var inQuotes = true;

        while (inQuotes
                   ? _bufferReader.TryAdvanceTo(quoteChar, advancePastDelimiter: false)
                   : _bufferReader.TryAdvanceToAny(_stopCharactersSearch, advancePastDelimiter: false))
        {
            var ch = _bufferReader.Current;
            if (ch == quoteChar)
            {
                if (_options.QuotesEscapeStyle == QuotesMode.DoubleQuotes
                    || (_options.QuotesEscapeStyle == QuotesMode.Backslash && _bufferReader.Past != '\\'))
                {
                    inQuotes = !inQuotes;
                    endQuotePosition = _bufferReader.Consumed;
                    quotesCount++;
                }
                if (_bufferReader.Advance(1) < 1)
                {
                    Trace("no data break");
                    return false;
                }
            }
            else if (!inQuotes && (_delimitersSearch.Contains(ch) || _endOfLineCharactersSearch.Contains(ch)))
            {
                return true;
            }
        }

        if (endQuotePosition < 1)
        {
            endQuotePosition = _bufferReader.Consumed;
        }
        return _noData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask<int> ReadNextBufferDataAsync(CancellationToken cancellationToken = default)
    {
        var buffer = _dynamicBuffer.Allocate();
        // The Read method has about 20% better performance than ReadAsync.
        var readBytes = AsyncRead
            ? await _streamReader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)
            : _streamReader.Read(buffer.Span);
        _dynamicBuffer.Commit(readBytes);
        return readBytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FieldInfo MoveToNextFieldInfo()
    {
        if ((uint)_fieldInfoLastIndex >= (uint)_fieldInfos.LongLength)
        {
            Array.Resize(ref _fieldInfos, _fieldInfoLastIndex + 1);
            _fieldInfos[^1] = new FieldInfo();
        }

        var field = _fieldInfos[_fieldInfoLastIndex++];
        field.Reset();
        return field;
    }

    /// <summary>
    /// Get column value.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns><c>True</c> if there are more records to read, <c>false</c> otherwise.</returns>
    public ReadOnlySpan<char> GetField(int columnIndex)
    {
        if ((uint)columnIndex >= (uint)_fieldInfoLastIndex)
        {
            return ReadOnlySpan<char>.Empty;
        }

        var fieldInfo = _fieldInfos[columnIndex];
        if (fieldInfo.HasQuotes)
        {
            if (fieldInfo.HasInnerQuotes)
            {
                var endPosition = _dynamicBuffer.GetPosition(fieldInfo.EndQuotePosition);
                return Unquote(
                    _dynamicBuffer.GetSequence(_dynamicBuffer.GetPosition(1, fieldInfo.Start), endPosition),
                    fieldInfo.QuoteCharacter);
            }
            else
            {
                var endPosition = _dynamicBuffer.GetPosition(fieldInfo.EndQuotePosition);
                return _dynamicBuffer.Slice(
                    _dynamicBuffer.GetPosition(1, fieldInfo.Start),
                    endPosition
                );
            }
        }
        return _dynamicBuffer.Slice(fieldInfo.Start, fieldInfo.Length);
    }

    /// <summary>
    /// Check if the column with the specific index is empty.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>Returns <c>true</c> if empty, <c>false</c> otherwise.</returns>
    public bool IsEmpty(int columnIndex)
    {
        if (columnIndex + 1 > _fieldInfoLastIndex)
        {
            return true;
        }
        var fieldInfo = _fieldInfos[columnIndex];
        if (fieldInfo.IsEmpty)
        {
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool NoFields() => (uint)_fieldInfoLastIndex == 0 || ((uint)_fieldInfoLastIndex == 1 && _fieldInfos[0].IsEmpty);

    #region Helpers

    /// <summary>
    /// Get Int32 value of the specific column.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>The value.</returns>
    public int GetInt32(int columnIndex) => int.Parse(GetField(columnIndex), provider: _options.Culture);

    /// <summary>
    /// Get decimal value of the specific column.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>The value.</returns>
    public decimal GetDecimal(int columnIndex) => decimal.Parse(GetField(columnIndex), provider: _options.Culture);

    /// <summary>
    /// Get <see cref="DateTime" /> value of the specific column.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>The value.</returns>
    public DateTime GetDateTime(int columnIndex) => DateTime.Parse(
        GetField(columnIndex),
        styles: DateTimeStyles.None,
        provider: _options.Culture);

    /// <summary>
    /// Get boolean value of the specific column.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>The value.</returns>
    public bool GetBoolean(int columnIndex) => bool.Parse(GetField(columnIndex));

    /// <summary>
    /// Get string value of the specific column.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>The value.</returns>
    public string GetString(int columnIndex) => GetField(columnIndex).ToString();

    #endregion

    /// <summary>
    /// Get the whole line.
    /// </summary>
    /// <returns>Line string.</returns>
    public ReadOnlySequence<char> GetLine()
    {
        if (_fieldInfoLastIndex < 1)
        {
            return ReadOnlySequence<char>.Empty;
        }
        var lastField = _fieldInfos[_fieldInfoLastIndex - 1];
        var end = _dynamicBuffer.GetPosition(lastField.Length, lastField.Start);
        return _dynamicBuffer.GetSequence(_fieldInfos[0].Start, end);
    }

    private async ValueTask FindDelimiterAsync(CancellationToken cancellationToken)
    {
        int readBytes;
        bool hasLine;
        SequenceReader<char> sequenceReader;
        ReadOnlySpan<char> line;
        do
        {
            readBytes = await ReadNextBufferDataAsync(cancellationToken)
                .ConfigureAwait(false);
            var currentSequence = _dynamicBuffer.GetSequence();
            sequenceReader = new SequenceReader<char>(currentSequence);
        }
        while (!(hasLine = sequenceReader.TryReadToAny(out line, _endOfLineCharacters))
               && readBytes > 0);

        // The single (or last) line may have no end of line characters - analyze what we have.
        if (!hasLine)
        {
            var remaining = sequenceReader.UnreadSequence;
            line = remaining.IsSingleSegment ? remaining.FirstSpan : remaining.ToArray();
        }

        if (TryDetectDelimiter(line, out var delimiter))
        {
            _options.Delimiters = [delimiter];
        }
        else if (_options.PreferredDelimiter.HasValue)
        {
            _options.Delimiters = [_options.PreferredDelimiter.Value];
        }
        else
        {
            throw new InvalidOperationException("Cannot determine delimiter. Please try to specify explicitly.");
        }

        InitStopCharacters();
    }

    /// <summary>
    /// Tries to detect delimiter that best matches to the specific string.
    /// </summary>
    /// <param name="line">Line to analyze.</param>
    /// <param name="delimiter">Delimiter or space if not found.</param>
    /// <returns><c>True</c> if found the best delimiter, <c>false</c> otherwise.</returns>
    public static bool TryDetectDelimiter(ReadOnlySpan<char> line, out char delimiter)
    {
        var autoDetectDelimitersCount = new int[_autoDetectDelimiters.Length];
        foreach (var ch in line)
        {
            var delimiterIndex = Array.IndexOf(_autoDetectDelimiters, ch);
            if (delimiterIndex > -1)
            {
                autoDetectDelimitersCount[delimiterIndex]++;
            }
        }

        var bestDelimiterCount = autoDetectDelimitersCount.Max();
        var bestDelimiterIndex = Array.IndexOf(autoDetectDelimitersCount, bestDelimiterCount);
        if (bestDelimiterIndex < 0 || bestDelimiterCount == 0)
        {
            delimiter = ' ';
            return false;
        }

        delimiter = _autoDetectDelimiters[bestDelimiterIndex];
        return true;
    }

    /// <summary>
    /// Resets current state and start reading from the beginning.
    /// </summary>
    public void Reset()
    {
        LineIndex = -1;
        _fieldInfoLastIndex = 0;
        _currentDelimiterPosition = 0;
        _dynamicBuffer.Clear();
        _bufferReader.Reset();
        if (_streamReader.BaseStream.CanSeek)
        {
            _streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
        }
        _streamReader.DiscardBufferedData();
        _parseState = StateReadField;
        _noData = false;
    }

    #region Escape

    internal ReadOnlySpan<char> Unquote(ReadOnlySequence<char> target, char quoteChar = '"')
    {
        if (_options.QuotesEscapeStyle == QuotesMode.DoubleQuotes)
        {
            return UnquoteDoubleQuotes(target, quoteChar);
        }
        if (_options.QuotesEscapeStyle == QuotesMode.Backslash)
        {
            return UnquoteBackslash(target);
        }
        return target.ToString();
    }

    public static ReadOnlySpan<char> UnquoteDoubleQuotes(ReadOnlySequence<char> target, char quoteChar = '"')
    {
        if (target.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }

        var endIndex = target.Length;
        var sequenceReader = target.First.Span[0] == quoteChar
            ? new SequenceReader<char>(target.Slice(1, endIndex - 2))
            : new SequenceReader<char>(target);

        var buffer = _stringBuilderPool.Get();
        try
        {
            while (sequenceReader.TryReadTo(out ReadOnlySpan<char> span, quoteChar))
            {
                buffer.Append(span);
                buffer.Append(quoteChar);
                if (sequenceReader.TryPeek(out var ch) && ch == quoteChar)
                {
                    sequenceReader.Advance(1);
                }
            }
            buffer.Append(sequenceReader.UnreadSequence);
            return buffer.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(buffer);
        }
    }

    public static ReadOnlySpan<char> UnquoteBackslash(ReadOnlySequence<char> target, char quoteChar = '"')
    {
        var endIndex = target.Length;
        var sequenceReader = target.First.Span[0] == quoteChar
            ? new SequenceReader<char>(target.Slice(1, --endIndex - 1))
            : new SequenceReader<char>(target);

        var buffer = _stringBuilderPool.Get();
        try
        {
            while (sequenceReader.TryReadTo(out ReadOnlySpan<char> span, '\\'))
            {
                buffer.Append(span);
                AppendEscapeCharacter(ref sequenceReader, buffer);
            }
            var unreadSequence = sequenceReader.UnreadSequence;
            buffer.Append(unreadSequence);
            return buffer.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(buffer);
        }
    }

    private static readonly SearchValues<char> _escapeRepeatChars = SearchValues.Create("\"'\\\n\r\t\v\0");

    private static void AppendEscapeCharacter(ref SequenceReader<char> reader, StringBuilder buffer)
    {
        // https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#string-escape-sequences.
        if (reader.TryPeek(out var ch))
        {
            if (_escapeRepeatChars.Contains(ch))
            {
                buffer.Append(ch);
            }
            reader.Advance(1);
        }
    }

    #endregion

    /// <inheritdoc />
    public override string ToString()
    {
        if (_streamReader.BaseStream is FileStream fileStream)
        {
            return Path.GetFileName(fileStream.Name).Trim();
        }
        return GetType().Name;
    }
}
