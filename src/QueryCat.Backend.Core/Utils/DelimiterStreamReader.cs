using System.Buffers;
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
     * csv.ReadAsync(); // Read header.
     * while (csv.ReadAsync())
     * {
     *     csv.GetInt32(0); // Read column #0 as int.
     *     csv.GetField(1); // Read column #1 as string.
     * }
     */
    private const int DefaultBufferSize = 0x5000;

    private static readonly char[] _autoDetectDelimiters = [',', '\t', ';', '|'];
    private static readonly char[] _endOfLineCharacters = ['\n', '\r'];

    public delegate void OnDelimiterDelegate(char ch, long position, out bool countField, out bool endLine);

    public OnDelimiterDelegate? OnDelimiter { get; set; }

    /// <summary>
    /// Use async read.
    /// </summary>
    public bool AsyncRead { get; set; } = OperatingSystem.IsBrowser();

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
        // 0: startIndex
        // 1: endIndex
        // 2: quotesCount
        private readonly long[] _indexes = new long[3];

        public long StartIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _indexes[0];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _indexes[0] = value;
        }

        public long EndIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _indexes[1];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _indexes[1] = value;
        }

        public long QuotesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _indexes[2];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _indexes[2] = value;
        }

        public char QuoteCharacter { get; set; }

        public bool HasQuotes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _indexes[2] > 0; }
        }

        public bool HasInnerQuotes => _indexes[2] > 2;

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _indexes[1] == _indexes[2]; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Reset()
        {
            new Span<long>(_indexes).Fill(0);
        }

        /// <inheritdoc />
        public override string ToString() => $"Position = {StartIndex}-{EndIndex}, Quotes = {HasQuotes}";
    }

    private readonly DynamicBuffer<char> _dynamicBuffer;
    private readonly StreamReader _streamReader;
    private readonly ReaderOptions _options;
    private char[] _stopCharacters = [];
    private SearchValues<char> _delimiters = SearchValues.Create(ReadOnlySpan<char>.Empty);
    private char[] _delimitersArray = [];
    private SearchValues<char> _quoteCharacters = SearchValues.Create(ReadOnlySpan<char>.Empty);

    // Stores positions of delimiters for columns.
    private FieldInfo[] _fieldInfos;
    private int _fieldInfoLastIndex;

    // Little optimization to prevent unnecessary DynamicBuffer.GetSequence() calls.
    private ReadOnlySequence<char> _currentSequence = ReadOnlySequence<char>.Empty;

    // Current position in a reading row. We need it in a case if read current row and need to
    // fetch new data to finish. The current position will contain the start index.
    private long _currentDelimiterPosition;
    private readonly DynamicBuffer<char>.DynamicBufferReader _dynamicBufferReader;

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
        _dynamicBufferReader = new DynamicBuffer<char>.DynamicBufferReader(_dynamicBuffer);

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
        _delimitersArray = _options.Delimiters;
        _delimiters = SearchValues.Create(_delimitersArray);
        _quoteCharacters = SearchValues.Create(_options.QuoteChars);
    }

    /// <summary>
    /// Get current line fields count.
    /// </summary>
    /// <returns>Fields count.</returns>
    public int GetFieldsCount() => _fieldInfoLastIndex - 1;

    /// <summary>
    /// Read the line ignoring delimiters and quotes.
    /// </summary>
    /// <returns><c>True</c> if the next data is available, <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueTask<bool> ReadLineAsync(CancellationToken cancellationToken = default)
        => ReadInternalAsync(lineMode: true, cancellationToken: cancellationToken);

    /// <summary>
    /// Read the line.
    /// </summary>
    /// <returns><c>True</c> if the next data is available, <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueTask<bool> ReadAsync(CancellationToken cancellationToken = default)
        => ReadInternalAsync(lineMode: false, cancellationToken: cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async ValueTask<bool> ReadInternalAsync(bool lineMode = false, CancellationToken cancellationToken = default)
    {
        _dynamicBuffer.Advance(_currentDelimiterPosition);
        _currentSequence = _dynamicBuffer.GetSequence();
        _currentDelimiterPosition = 0;
        var includeDelimiter = _options.IncludeDelimiter;

        if (_dynamicBuffer.IsEmpty)
        {
            await ReadNextBufferDataAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        if (_options.Delimiters.Length == 0)
        {
            await FindDelimiterAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        bool isInQuotes = false, // Are we within quotes?
            quotesMode = false, // Set when first field char is quote.
            fieldStart = true; // Indicates that we are at field start.
        _fieldInfoLastIndex = 0;
        var currentField = MoveToNextFieldInfo();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stopCharactersLocal = (ReadOnlySpan<char>)_stopCharacters;
            _dynamicBufferReader.Reset();
            _dynamicBufferReader.Advance(_currentDelimiterPosition);
            while (_currentDelimiterPosition < _dynamicBuffer.Size)
            {
                // Skip extra spaces (or any delimiters).
                if (_options.SkipRepeatedDelimiters)
                {
                    while (_delimiters.Contains(_dynamicBufferReader.Next))
                    {
                        _currentDelimiterPosition++;
                        _dynamicBufferReader.Advance(1);
                        currentField.StartIndex = _currentDelimiterPosition;
                    }
                }

                // Advance to any stop character or quote (if in a quote mode).
                var hasAdvanced = !isInQuotes
                    ? _dynamicBufferReader.TryAdvanceToAny(stopCharactersLocal, advancePastDelimiter: false)
                    : _dynamicBufferReader.TryAdvanceTo(currentField.QuoteCharacter, advancePastDelimiter: false);
                if (!hasAdvanced)
                {
                    _currentDelimiterPosition = _dynamicBufferReader.Consumed;
                    break;
                }
                var ch = _dynamicBufferReader.Current;
                _dynamicBufferReader.Advance(1);

                // Delimiters.
                if (!lineMode && _delimiters.Contains(ch))
                {
                    _currentDelimiterPosition = _dynamicBufferReader.Consumed;

                    if (!isInQuotes && _currentDelimiterPosition > 0)
                    {
                        bool addField = true, completeLine = false;
                        OnDelimiter?.Invoke(ch, _currentDelimiterPosition, out addField, out completeLine);
                        if (addField)
                        {
                            currentField.EndIndex = includeDelimiter ? _currentDelimiterPosition : _currentDelimiterPosition - 1;
                            currentField = MoveToNextFieldInfo();
                            fieldStart = true;
                            currentField.StartIndex = includeDelimiter ? _currentDelimiterPosition - 1 : _currentDelimiterPosition;
                            currentField.QuotesCount = 0;
                            quotesMode = false;
                        }
                        if (completeLine)
                        {
                            return true;
                        }
                    }
                }
                // Quotes.
                else if (isInQuotes || _quoteCharacters.Contains(ch))
                {
                    if (fieldStart)
                    {
                        var hasOnlyWhitespacesBeforeDelimiter =
                            !_options.EnableQuotesModeOnFieldStart // Bypass.
                            || (_options.EnableQuotesModeOnFieldStart && HasOnlyWhitespaceChars(currentField.StartIndex));
                        if (hasOnlyWhitespacesBeforeDelimiter)
                        {
                            quotesMode = true;
                            isInQuotes = true;
                            currentField.QuoteCharacter = ch;
                            currentField.QuotesCount++;
                            _currentDelimiterPosition = _dynamicBufferReader.Consumed;
                            currentField.StartIndex = includeDelimiter ? _currentDelimiterPosition : _currentDelimiterPosition - 1;
                        }

                        fieldStart = false;
                    }
                    else if (quotesMode)
                    {
                        if (_options.QuotesEscapeStyle == QuotesMode.DoubleQuotes)
                        {
                            isInQuotes = !isInQuotes;
                        }
                        else if (_options.QuotesEscapeStyle == QuotesMode.Backslash)
                        {
                            // Process \" case.
                            var prevCh = _dynamicBufferReader.GetPosition(-2).Value;
                            if (prevCh != '\\')
                            {
                                isInQuotes = !isInQuotes;
                            }
                        }
                        currentField.QuotesCount++;
                    }
                }
                // End of line.
                else if (_options.CompleteOnEndOfLine && ch is '\n' or '\r')
                {
                    if (!isInQuotes)
                    {
                        _currentDelimiterPosition = _dynamicBufferReader.Consumed;
                        currentField.EndIndex = _currentDelimiterPosition - 1;
                        MoveToNextFieldInfo();
                        fieldStart = true;

                        // Process /r/n Windows line end case.
                        if (ch == '\r'
                            && _dynamicBufferReader.Current == '\n')
                        {
                            _currentDelimiterPosition++;
                        }

                        // Skip empty line and try to read next.
                        if (_options.SkipEmptyLines && IsEmpty())
                        {
                            _fieldInfoLastIndex = 0;
                            currentField = MoveToNextFieldInfo();
                            currentField.StartIndex = _currentDelimiterPosition;
                            if (!_dynamicBufferReader.End)
                            {
                                _dynamicBufferReader.Advance(1);
                            }
                            continue;
                        }

                        return true;
                    }

                    _currentDelimiterPosition = _dynamicBufferReader.Consumed;
                    break;
                }
            }

            var remain = _dynamicBufferReader.Remaining;
            var readBytes = await ReadNextBufferDataAsync(cancellationToken)
                .ConfigureAwait(false);
            if (readBytes < 1)
            {
                _currentDelimiterPosition += remain;
                break;
            }
        }

        // We are at the end of the stream. Update remain index and exit.
        currentField.EndIndex = _currentDelimiterPosition;
        // Move next field index next to correct calculate total columns count.
        MoveToNextFieldInfo();
        return !IsEmpty();
    }

    private bool HasOnlyWhitespaceChars(long startIndex)
    {
        var count = _dynamicBufferReader.Consumed - startIndex - 1;

        if (count < 1)
        {
            return true;
        }

        _dynamicBufferReader.Rewind(1);

        // Go backward and analyze.
        var hasOnlyWhitespaces = true;
        int i;
        for (i = 0; i < count; i++)
        {
            _dynamicBufferReader.Rewind(1);
            var ch = _dynamicBufferReader.Current;
            if (ch == '\0')
            {
                break;
            }
            if (_delimiters.Contains(ch))
            {
                continue;
            }
            if (!char.IsWhiteSpace(ch))
            {
                hasOnlyWhitespaces = false;
                break;
            }
        }

        // Restore sequence position.
        _dynamicBufferReader.Advance(i + 1);

        return hasOnlyWhitespaces;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEmpty()
        => _fieldInfoLastIndex <= 2 && _fieldInfos[0].EndIndex == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask<int> ReadNextBufferDataAsync(CancellationToken cancellationToken = default)
    {
        var buffer = _dynamicBuffer.Allocate();
        // The Read method has about 20% better performance than ReadAsync.
        var readBytes = AsyncRead
            ? await _streamReader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)
            : _streamReader.Read(buffer.Span);
        _dynamicBuffer.Commit(readBytes);
        _currentSequence = _dynamicBuffer.GetSequence();
        return readBytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FieldInfo MoveToNextFieldInfo()
    {
        if (_fieldInfoLastIndex >= _fieldInfos.LongLength)
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
        if (columnIndex + 1 > _fieldInfoLastIndex)
        {
            return ReadOnlySpan<char>.Empty;
        }

        ref var fieldInfo = ref _fieldInfos[columnIndex];
        if (fieldInfo.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }
        var valueSequence = _currentSequence.Slice(fieldInfo.StartIndex, fieldInfo.EndIndex - fieldInfo.StartIndex);
        if (fieldInfo.HasQuotes)
        {
            if (!fieldInfo.HasInnerQuotes)
            {
                valueSequence = valueSequence.Slice(1, valueSequence.Length - 2);
            }
            else
            {
                return Unquote(valueSequence, fieldInfo.QuoteCharacter);
            }
        }
        return valueSequence.IsSingleSegment ? valueSequence.FirstSpan : valueSequence.ToArray();
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
        ref var fieldInfo = ref _fieldInfos[columnIndex];
        if (fieldInfo.IsEmpty)
        {
            return true;
        }
        if (fieldInfo.HasQuotes && fieldInfo.EndIndex - fieldInfo.StartIndex - fieldInfo.QuotesCount == 0)
        {
            return true;
        }
        return false;
    }

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
        if (_fieldInfoLastIndex < 2)
        {
            return ReadOnlySequence<char>.Empty;
        }
        var startIndex = _fieldInfos[0].StartIndex;
        var endIndex = _fieldInfos[_fieldInfoLastIndex - 2].EndIndex;
        return _currentSequence.Slice(startIndex, endIndex - startIndex);
    }

    private async ValueTask FindDelimiterAsync(CancellationToken cancellationToken)
    {
        int readBytes;
        SequenceReader<char> sequenceReader;
        ReadOnlySpan<char> line;
        do
        {
            readBytes = await ReadNextBufferDataAsync(cancellationToken)
                .ConfigureAwait(false);
            _currentSequence = _dynamicBuffer.GetSequence();
            sequenceReader = new SequenceReader<char>(_currentSequence);
        }
        while (!sequenceReader.TryReadToAny(out line, _endOfLineCharacters)
               && readBytes > 0);

        if (_options.DetectDelimiter)
        {
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
        var bestDelimiterIndex = Array.IndexOf(autoDetectDelimitersCount, autoDetectDelimitersCount.Max());
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
        _fieldInfoLastIndex = 0;
        _currentDelimiterPosition = 0;
        _currentSequence = ReadOnlySequence<char>.Empty;
        _dynamicBuffer.Clear();
        _streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
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
        var endIndex = target.Length;
        var sequenceReader = target.First.Span[0] == quoteChar
            ? new SequenceReader<char>(target.Slice(1, --endIndex - 1))
            : new SequenceReader<char>(target);

        var buffer = new StringBuilder(capacity: (int)endIndex + 1);
        while (sequenceReader.TryReadTo(out ReadOnlySpan<char> span, quoteChar))
        {
            buffer.Append(span);
            if (sequenceReader.TryPeek(out var ch) && ch == quoteChar)
            {
                buffer.Append(quoteChar);
                sequenceReader.Advance(1);
            }
        }
        buffer.Append(sequenceReader.UnreadSequence);
        return GetSpanFromStringBuilder(buffer);
    }

    public static ReadOnlySpan<char> UnquoteBackslash(ReadOnlySequence<char> target, char quoteChar = '"')
    {
        var endIndex = target.Length;
        var sequenceReader = target.First.Span[0] == quoteChar
            ? new SequenceReader<char>(target.Slice(1, --endIndex - 1))
            : new SequenceReader<char>(target);

        var buffer = new StringBuilder((int)endIndex + 1);
        while (sequenceReader.TryReadTo(out ReadOnlySpan<char> span, '\\'))
        {
            buffer.Append(span);
            AppendEscapeCharacter(ref sequenceReader, buffer);
        }
        var unreadSequence = sequenceReader.UnreadSequence;
        buffer.Append(unreadSequence);
        return GetSpanFromStringBuilder(buffer);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static ReadOnlySpan<char> GetSpanFromStringBuilder(StringBuilder sb)
    {
        var chunksEnumerator = sb.GetChunks().GetEnumerator();
        var hasFirstChunk = chunksEnumerator.MoveNext();
        if (!hasFirstChunk)
        {
            return ReadOnlySpan<char>.Empty;
        }
        var span = chunksEnumerator.Current.Span;
        var hasSecondChunk = chunksEnumerator.MoveNext();
        if (!hasSecondChunk)
        {
            return span;
        }
        return sb.ToString();
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
