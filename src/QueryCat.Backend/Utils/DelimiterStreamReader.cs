using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Options for <see cref="DelimiterStreamReader" />.
    /// </summary>
    public sealed class ReaderOptions
    {
        /// <summary>
        /// Quote character.
        /// </summary>
        public char[] QuoteChars { get; init; } = Array.Empty<char>();

        /// <summary>
        /// Columns delimiters.
        /// </summary>
        public char[] Delimiters { get; init; } = Array.Empty<char>();

        /// <summary>
        /// Can a delimiter be repeated. Can be useful for example for whitespace delimiter.
        /// </summary>
        public bool DelimitersCanRepeat { get; init; } = false;

        /// <summary>
        /// Buffer size.
        /// </summary>
        public int BufferSize { get; init; } = DefaultBufferSize;

        /// <summary>
        /// Do not take into account empty lines.
        /// </summary>
        public bool SkipEmptyLines { get; init; } = true;

        /// <summary>
        /// Culture to use for parse.
        /// </summary>
        public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;
    }

    private struct FieldInfo
    {
        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public int QuotesCount { get; set; }

        public char QuoteCharacter { get; set; }

        public bool HasQuotes => QuotesCount > 0;

        public bool HasInnerQuotes => QuotesCount > 2;

        public bool IsEmpty => EndIndex - StartIndex < 2;

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
    private readonly char[] _stopCharacters;

    // Stores positions of delimiters for columns.
    private FieldInfo[] _fieldInfos = new FieldInfo[32];
    private int _fieldInfoLastIndex;

    // Little optimization to prevent unnecessary DynamicBuffer.GetSequence() calls.
    private ReadOnlySequence<char> _currentSequence = ReadOnlySequence<char>.Empty;

    // Current position in a reading row. We need it in a case if read current row and need to
    // fetch new data to finish. The current position will contain the start index.
    private int _currentDelimiterPosition;

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

        // Stop characters.
        _stopCharacters = _options.Delimiters
            .Union(_options.QuoteChars)
            .Union(new[] { '\n', '\r' })
            .ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private char GetNextCharacter()
    {
        if (_dynamicBuffer.Size <= _currentDelimiterPosition)
        {
            ReadNextBufferData();
        }
        if (_dynamicBuffer.Size > _currentDelimiterPosition)
        {
            return _dynamicBuffer.GetAt(_currentDelimiterPosition);
        }
        return '\0';
    }

    /// <summary>
    /// Get current line fields count.
    /// </summary>
    /// <returns></returns>
    public int GetFieldsCount() => _fieldInfoLastIndex - 1;

    /// <summary>
    /// Read the line ignoring delimiters and quotes.
    /// </summary>
    /// <returns><c>True</c> if the next data is available, <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadLine() => ReadInternal(lineMode: true);

    /// <summary>
    /// Read the line.
    /// </summary>
    /// <returns><c>True</c> if the next data is available, <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        int readBytes;
        bool isInQuotes = false, // Are we within quotes?
            quotesMode = false, // Set when first field char is quote.
            fieldStart = true; // Indicates that we are at field start.
        SequenceReader<char> sequenceReader;
        _fieldInfoLastIndex = 0;
        ref FieldInfo currentField = ref GetNextFieldInfo();
        do
        {
            _currentSequence = _dynamicBuffer.GetSequence();
            sequenceReader = new SequenceReader<char>(_currentSequence);
            sequenceReader.Advance(_currentDelimiterPosition);
            while (_currentDelimiterPosition < _dynamicBuffer.Size)
            {
                // Skip extra spaces (or any delimiters).
                if (_options.DelimitersCanRepeat)
                {
                    while (Array.IndexOf(_options.Delimiters, GetNextCharacter()) > -1)
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
                    _currentDelimiterPosition = (int)sequenceReader.Consumed;
                    break;
                }
                sequenceReader.TryPeek(out char ch);
                sequenceReader.Advance(1);

                // Quotes.
                if (Array.IndexOf(_options.QuoteChars, ch) > -1)
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
                        isInQuotes = !isInQuotes;
                        if (quotesMode)
                        {
                            currentField.QuotesCount++;
                        }
                    }
                }
                // Delimiters.
                else if (!lineMode && Array.IndexOf(_options.Delimiters, ch) > -1)
                {
                    _currentDelimiterPosition = (int)sequenceReader.Consumed;
                    if (!isInQuotes && _currentDelimiterPosition > 1)
                    {
                        currentField.EndIndex = _currentDelimiterPosition;
                        currentField = ref GetNextFieldInfo();
                        fieldStart = true;
                        currentField.StartIndex = _currentDelimiterPosition;
                        currentField.QuotesCount = 0;
                        quotesMode = false;
                    }
                }
                // End of line.
                else if (ch == '\n' || ch == '\r')
                {
                    if (!isInQuotes)
                    {
                        _currentDelimiterPosition = (int)sequenceReader.Consumed;
                        currentField.EndIndex = _currentDelimiterPosition;
                        currentField = ref GetNextFieldInfo();
                        fieldStart = true;

                        // Process /r/n Windows line end case.
                        if (ch == '\r' && GetNextCharacter() == '\n')
                        {
                            _currentDelimiterPosition++;
                        }

                        // Skip empty line and try to read next.
                        if (_options.SkipEmptyLines && IsEmpty())
                        {
                            sequenceReader.Advance(_currentDelimiterPosition);
                            _fieldInfoLastIndex = 1;
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
        currentField.EndIndex = _currentDelimiterPosition + 1;
        return !IsEmpty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private bool IsEmpty()
        => _fieldInfoLastIndex == 1 && _fieldInfos[0].EndIndex == 1;

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
        if (_fieldInfoLastIndex > _fieldInfos.Length)
        {
            Array.Resize(ref _fieldInfos, _fieldInfoLastIndex);
        }

#if DEBUG
        _fieldInfos[_fieldInfoLastIndex].Reset();
#endif
        return ref _fieldInfos[_fieldInfoLastIndex++];
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

        ref FieldInfo fieldInfo = ref _fieldInfos[columnIndex];
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
        ref FieldInfo fieldInfo = ref _fieldInfos[columnIndex];
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
        if (_fieldInfoLastIndex < 1)
        {
            return ReadOnlySequence<char>.Empty;
        }
        return _currentSequence.Slice(
            _fieldInfos[0].StartIndex, _fieldInfos[_fieldInfoLastIndex - 2].EndIndex - 1);
    }

    internal static ReadOnlySpan<char> Unquote(string target, char quoteChar = '"')
    {
        return Unquote(new ReadOnlySequence<char>(target.AsMemory()), quoteChar);
    }

    internal static ReadOnlySpan<char> Unquote(ReadOnlySequence<char> target, char quoteChar = '"')
    {
        int endIndex = (int)target.Length;
        var valueReader = target.First.Span[0] == quoteChar
            ? new SequenceReader<char>(target.Slice(1, --endIndex - 1))
            : new SequenceReader<char>(target);

        var buffer = new char[endIndex + 1].AsSpan();
        var lastBufferIndex = 0;
        while (valueReader.TryReadTo(out ReadOnlySpan<char> span, quoteChar))
        {
            if (span.IsEmpty)
            {
                break;
            }
            span.CopyTo(buffer.Slice(lastBufferIndex, span.Length));
            lastBufferIndex += span.Length;
            buffer[lastBufferIndex++] = quoteChar;
            valueReader.Advance(1);
        }
        var unreadSequence = valueReader.UnreadSequence;
        var unreadSequenceLength = (int)unreadSequence.Length;
        unreadSequence.CopyTo(buffer.Slice(lastBufferIndex, unreadSequenceLength));
        lastBufferIndex += unreadSequenceLength;
        return buffer.Slice(0, lastBufferIndex);
    }

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
