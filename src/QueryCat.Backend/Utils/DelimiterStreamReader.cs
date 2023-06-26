using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace QueryCat.Backend.Utils;

/// <summary>
/// Simple text parser with the delimiters. Can be used to parse CSV, DSV, stdin stream, etc.
/// </summary>
public class DelimiterStreamReader
{
    /*
     * The basic use case:
     *
     * var csv = new DelimiterStreamReader(new StreamReader(file));
     * csv.Read(); // Read header.
     * while (csv.Read())
     * {
     *     csv.GetInt32(0); // Read column #0 as int.
     *     csv.GetField(1); // Read column #1 as string.
     * }
     */
    private const int DefaultBufferSize = 0x4000;

    private static readonly char[] AutoDetectDelimiters = { ',', '\t', ';', '|' };
    private static readonly char[] EndOfLineCharacters = { '\n', '\r' };

    public delegate void OnDelimiterDelegate(char ch, long pos, out bool countField, out bool endLine);

    public OnDelimiterDelegate? OnDelimiter { get; set; }

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
        public char[] QuoteChars { get; set; } = Array.Empty<char>();

        /// <summary>
        /// Columns delimiters.
        /// </summary>
        public char[] Delimiters { get; set; } = Array.Empty<char>();

        /// <summary>
        /// Get the preferred delimiter if we cannot determine any from line.
        /// </summary>
        public char? PreferredDelimiter { get; set; }

        /// <summary>
        /// Can a delimiter be repeated. Can be useful for example for whitespace delimiter.
        /// </summary>
        public bool DelimitersCanRepeat { get; set; }

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

    private struct FieldInfo
    {
        public long StartIndex { get; set; }

        public long EndIndex { get; set; }

        public int QuotesCount { get; set; }

        public char QuoteCharacter { get; set; }

        public bool HasQuotes => QuotesCount > 0;

        public bool HasInnerQuotes => QuotesCount > 2;

        public bool IsEmpty => EndIndex - StartIndex < 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            StartIndex = 0;
            EndIndex = 0;
            QuotesCount = 0;
        }

        /// <inheritdoc />
        public override string ToString() => $"Position = {StartIndex}-{EndIndex}, Quotes = {HasQuotes}";
    }

    private readonly DynamicBuffer<char> _dynamicBuffer;
    private readonly StreamReader _streamReader;
    private readonly ReaderOptions _options;
    private char[] _stopCharacters = Array.Empty<char>();

    // Stores positions of delimiters for columns.
    private FieldInfo[] _fieldInfos = new FieldInfo[32];
    private int _fieldInfoLastIndex;

    // Little optimization to prevent unnecessary DynamicBuffer.GetSequence() calls.
    private ReadOnlySequence<char> _currentSequence = ReadOnlySequence<char>.Empty;

    // Current position in a reading row. We need it in a case if read current row and need to
    // fetch new data to finish. The current position will contain the start index.
    private long _currentDelimiterPosition;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="streamReader">Stream reader.</param>
    /// <param name="options">Reader options.</param>
    public DelimiterStreamReader(StreamReader streamReader, ReaderOptions? options = null)
    {
        _streamReader = streamReader;
        _options = options ?? new ReaderOptions();
        _dynamicBuffer = new DynamicBuffer<char>(_options.BufferSize);
        InitStopCharacters();
    }

    private void InitStopCharacters()
    {
        var endOfLineCharacters = _options.CompleteOnEndOfLine ? EndOfLineCharacters : Array.Empty<char>();
        _stopCharacters = _options.Delimiters
            .Union(_options.QuoteChars)
            .Union(endOfLineCharacters)
            .ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private bool EnsureHasAdvanceData(ref SequenceReader<char> sequenceReader, int advance = 1)
    {
        if ((ulong)_dynamicBuffer.Size < (ulong)(_currentDelimiterPosition + advance))
        {
            var readCount = ReadNextBufferData();
            if (readCount == 0)
            {
                return false;
            }
            _currentSequence = _dynamicBuffer.GetSequence();
            sequenceReader = new SequenceReader<char>(_currentSequence);
            sequenceReader.Advance(_currentDelimiterPosition);
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private bool GetNextCharacter(out char nextChar)
    {
        if ((ulong)_dynamicBuffer.Size > (ulong)_currentDelimiterPosition)
        {
            nextChar = _dynamicBuffer.GetAt((int)_currentDelimiterPosition);
            return true;
        }
        nextChar = '\0';
        return false;
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
    public bool ReadLine() => ReadInternal(lineMode: true);

    /// <summary>
    /// Read the line.
    /// </summary>
    /// <returns><c>True</c> if the next data is available, <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Read() => ReadInternal(lineMode: false);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private bool ReadInternal(bool lineMode = false)
    {
        _dynamicBuffer.Advance(_currentDelimiterPosition);
        _currentDelimiterPosition = 0;
        if (_dynamicBuffer.IsEmpty)
        {
            ReadNextBufferData();
        }

        if (_options.Delimiters.Length == 0)
        {
            FindDelimiter();
        }

        int readBytes;
        bool isInQuotes = false, // Are we within quotes?
            quotesMode = false, // Set when first field char is quote.
            fieldStart = true; // Indicates that we are at field start.
        SequenceReader<char> sequenceReader;
        _fieldInfoLastIndex = 0;
        ref var currentField = ref GetNextFieldInfo();
        do
        {
            _currentSequence = _dynamicBuffer.GetSequence();
            sequenceReader = new SequenceReader<char>(_currentSequence);
            sequenceReader.Advance(_currentDelimiterPosition);
            while ((ulong)_currentDelimiterPosition < (ulong)_dynamicBuffer.Size)
            {
                // Skip extra spaces (or any delimiters).
                if (_options.DelimitersCanRepeat)
                {
                    while (EnsureHasAdvanceData(ref sequenceReader)
                           && GetNextCharacter(out var nextChar)
                           && Array.IndexOf(_options.Delimiters, nextChar) > -1)
                    {
                        _currentDelimiterPosition++;
                        sequenceReader.Advance(1);
                    }
                    currentField.StartIndex = _currentDelimiterPosition;
                }

                // Advance to any stop character or quote (if in a quote mode).
                var hasAdvanced = !isInQuotes
                    ? sequenceReader.TryAdvanceToAny(_stopCharacters, advancePastDelimiter: false)
                    : sequenceReader.TryAdvanceTo(currentField.QuoteCharacter, advancePastDelimiter: false);
                if (!hasAdvanced)
                {
                    _currentDelimiterPosition = sequenceReader.Consumed;
                    break;
                }
                var ch = sequenceReader.CurrentSpan[sequenceReader.CurrentSpanIndex];
                sequenceReader.Advance(1);

                // Quotes.
                if (isInQuotes || Array.IndexOf(_options.QuoteChars, ch) > -1)
                {
                    if (fieldStart)
                    {
                        quotesMode = true;
                        isInQuotes = true;
                        currentField.QuoteCharacter = ch;
                        currentField.QuotesCount++;
                        fieldStart = false;
                    }
                    else
                    {
                        if (_options.QuotesEscapeStyle == QuotesMode.DoubleQuotes)
                        {
                            isInQuotes = !isInQuotes;
                        }
                        else if (_options.QuotesEscapeStyle == QuotesMode.Backslash)
                        {
                            // Process \" case.
                            sequenceReader.Rewind(2);
                            sequenceReader.TryPeek(out var prevCh);
                            sequenceReader.Advance(2);
                            if (prevCh != '\\')
                            {
                                isInQuotes = !isInQuotes;
                            }
                        }
                        if (quotesMode)
                        {
                            currentField.QuotesCount++;
                        }
                    }
                }
                // Delimiters.
                else if (!lineMode && Array.IndexOf(_options.Delimiters, ch) > -1)
                {
                    _currentDelimiterPosition = sequenceReader.Consumed;
                    if (!isInQuotes && (ulong)_currentDelimiterPosition > 0)
                    {
                        bool addField = true, completeLine = false;
                        OnDelimiter?.Invoke(ch, _currentDelimiterPosition, out addField, out completeLine);
                        if (addField)
                        {
                            currentField.EndIndex = _options.IncludeDelimiter ? _currentDelimiterPosition + 1 : _currentDelimiterPosition;
                            currentField = ref GetNextFieldInfo();
                            fieldStart = true;
                            currentField.StartIndex = _options.IncludeDelimiter ? _currentDelimiterPosition - 1 : _currentDelimiterPosition;
                            currentField.QuotesCount = 0;
                            quotesMode = false;
                        }
                        if (completeLine)
                        {
                            _currentDelimiterPosition = sequenceReader.Consumed;
                            return true;
                        }
                    }
                }
                // End of line.
                else if (_options.CompleteOnEndOfLine && ch is '\n' or '\r')
                {
                    if (!isInQuotes)
                    {
                        _currentDelimiterPosition = sequenceReader.Consumed;
                        currentField.EndIndex = _currentDelimiterPosition;
                        currentField = ref GetNextFieldInfo();
                        fieldStart = true;

                        // Process /r/n Windows line end case.
                        if (ch == '\r'
                            && EnsureHasAdvanceData(ref sequenceReader)
                            && sequenceReader.IsNext('\n'))
                        {
                            _currentDelimiterPosition++;
                        }

                        // Skip empty line and try to read next.
                        if (_options.SkipEmptyLines && IsEmpty())
                        {
                            _fieldInfoLastIndex = 0;
                            currentField = ref GetNextFieldInfo();
                            currentField.StartIndex = _currentDelimiterPosition;
                            if (!sequenceReader.End)
                            {
                                sequenceReader.Advance(1);
                            }
                            continue;
                        }

                        return true;
                    }

                    _currentDelimiterPosition = sequenceReader.Consumed;
                    break;
                }
            }

            readBytes = ReadNextBufferData();
        }
        while ((ulong)readBytes > 0);

        // We are at the end of the stream. Update remain index and exit.
        _currentDelimiterPosition += sequenceReader.Remaining;
        currentField.EndIndex = _currentDelimiterPosition + 1;
        // Move next field index next to correct calculate total columns count.
        currentField = ref GetNextFieldInfo();
        return !IsEmpty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private bool IsEmpty()
        => _fieldInfoLastIndex <= 2 && _fieldInfos[0].EndIndex == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int ReadNextBufferData()
    {
        if (_streamReader.EndOfStream)
        {
            return 0;
        }

        var buffer = _dynamicBuffer.Allocate();
        var readBytes = _streamReader.Read(buffer);
        _dynamicBuffer.Commit(readBytes);
        return readBytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ref FieldInfo GetNextFieldInfo()
    {
        if ((ulong)_fieldInfoLastIndex >= (ulong)_fieldInfos.Length)
        {
            Array.Resize(ref _fieldInfos, _fieldInfoLastIndex + 1);
        }

        _fieldInfos[_fieldInfoLastIndex].Reset();
        return ref _fieldInfos[_fieldInfoLastIndex++];
    }

    /// <summary>
    /// Get column value.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <returns><c>True</c> if there are more records to read, <c>false</c> otherwise.</returns>
    public ReadOnlySpan<char> GetField(int columnIndex)
    {
        if ((ulong)columnIndex + 1 > (ulong)_fieldInfoLastIndex)
        {
            return ReadOnlySpan<char>.Empty;
        }

        ref var fieldInfo = ref _fieldInfos[columnIndex];
        if (fieldInfo.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }
        var valueSequence = _currentSequence.Slice(fieldInfo.StartIndex, fieldInfo.EndIndex - fieldInfo.StartIndex - 1);
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
    public DateTime GetDateTime(int columnIndex) => DateTime.Parse(GetField(columnIndex),
        styles: DateTimeStyles.None, provider: _options.Culture);

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
        return _currentSequence.Slice(
            _fieldInfos[0].StartIndex, _fieldInfos[_fieldInfoLastIndex - 2].EndIndex - 1);
    }

    private void FindDelimiter()
    {
        SequenceReader<char> sequenceReader;
        ReadOnlySpan<char> line;
        do
        {
            _currentSequence = _dynamicBuffer.GetSequence();
            sequenceReader = new SequenceReader<char>(_currentSequence);
        }
        while (!sequenceReader.TryReadToAny(out line, EndOfLineCharacters)
               && ReadNextBufferData() > 0);

        if (!TryDetectDelimiter(line, out var delimiter))
        {
            if (_options.PreferredDelimiter.HasValue)
            {
                delimiter = _options.PreferredDelimiter.Value;
            }
            else
            {
                throw new InvalidOperationException("Cannot determine delimiter. Please try to specify explicitly.");
            }
        }

        _options.Delimiters = new[] { delimiter };
        InitStopCharacters();
    }

    /// <summary>
    /// Tries to detect delimiter that best matches to the specific string.
    /// </summary>
    /// <param name="line">Line to analyze.</param>
    /// <param name="delimiter">Delimiter or space if not found.</param>
    /// <returns><c>True</c> if found best delimiter, <c>false</c> otherwise.</returns>
    public static bool TryDetectDelimiter(ReadOnlySpan<char> line, out char delimiter)
    {
        var autoDetectDelimitersCount = new int[AutoDetectDelimiters.Length];
        for (var i = 0; i < line.Length; i++)
        {
            var delimiterIndex = Array.IndexOf(AutoDetectDelimiters, line[i]);
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

        delimiter = AutoDetectDelimiters[bestDelimiterIndex];
        return true;
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

    internal static ReadOnlySpan<char> UnquoteDoubleQuotes(ReadOnlySequence<char> target, char quoteChar = '"')
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

    internal static ReadOnlySpan<char> UnquoteBackslash(ReadOnlySequence<char> target, char quoteChar = '"')
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

    private static readonly char[] EscapeRepeatChars = { '"', '\'', '\\', '\n', '\r', '\t', '\v', '\0' };

    private static void AppendEscapeCharacter(ref SequenceReader<char> reader, StringBuilder buffer)
    {
        // https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#string-escape-sequences.
        if (reader.TryPeek(out var ch))
        {
            if (Array.IndexOf(EscapeRepeatChars, ch) > -1)
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
