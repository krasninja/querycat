using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Output rows into text writer. The result will be formatted
/// using spaces.
/// </summary>
public sealed class TextTableOutput : RowsOutput, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Table style.
    /// </summary>
    public enum Style
    {
        Table1,
        Table2,
        NoSpaceTable,
        Card,
        SolidTable,
    }

    private readonly Stream _stream;
    private int[] _totalMaxLineLength = [];
    private StreamWriter _streamWriter = StreamWriter.Null;
    private bool _isSingleValue;
    private int _maxColumnNameWidth = 10;

    private readonly bool _hasHeader;
    private readonly string _separator;
    private readonly string _separatorWithSpace;
    private readonly string _floatNumberFormat;

    private readonly Action _onHeader;
    private readonly Action<VariantValue[]> _onWrite;
    private readonly Action _onFooter;

    private int[] _columnsLengths = [];

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(TextTableOutput));

    /// <summary>
    /// Columns separator.
    /// </summary>
    public string Separator => _separator;

#pragma warning disable SA1203
    private const char BoxBottomTop = '\u2502';
    private const char BoxLeftBottomRight = '\u252c';
    private const char BoxBottomRight = '\u250c';
    private const char BoxLeftBottom = '\u2510';
    private const char BoxLeftRight = '\u2500';
    private const char BoxBottomRightTop = '\u251c';
    private const char BoxLeftBottomTop = '\u2524';
    private const char BoxLeftBottomRightTop = '\u253c';
    private const char BoxRightTop = '\u2514';
    private const char BoxLeftTop = '\u2518';
    private const char BoxLeftTopRight = '\u2534';
#pragma warning restore SA1203

    public TextTableOutput(
        Stream stream,
        bool hasHeader = true,
        string? separator = null,
        Style style = Style.Table1,
        string? floatNumberFormat = null)
    {
        _stream = stream;
        _hasHeader = hasHeader;
        _floatNumberFormat = floatNumberFormat ?? VariantValue.FloatNumberFormat;
        Options = new RowsOutputOptions
        {
            RequiresColumnsLengthAdjust = true,
        };

        if (style == Style.Card)
        {
            _onHeader = OnCardHeader;
            _onWrite = OnCardWrite;
            _onFooter = OnCardFooter;
            _separator = separator ?? ":";
        }
        else if (style == Style.Table1)
        {
            _onHeader = OnTable1Header;
            _onWrite = OnTable1Write;
            _onFooter = OnTable1Footer;
            _separator = separator ?? "|";
        }
        else if (style == Style.Table2)
        {
            _onHeader = OnTable2Header;
            _onWrite = OnTable2Write;
            _onFooter = OnTable2Footer;
            _separator = separator ?? "|";
        }
        else if (style == Style.NoSpaceTable)
        {
            _onHeader = OnNoSpaceTableHeader;
            _onWrite = OnNoSpaceTableWrite;
            _onFooter = OnNoSpaceTableFooter;
            _separator = separator ?? "|";
        }
        else if (style == Style.SolidTable)
        {
            _onHeader = OnSolidTableHeader;
            _onWrite = OnSolidTableWrite;
            _onFooter = OnSolidTableFooter;
            _separator = BoxBottomTop.ToString();
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(style));
        }

        _separatorWithSpace = !string.IsNullOrEmpty(_separator) ? _separator + " " : string.Empty;
    }

    /// <summary>
    /// Simple wrapper to write to string builder as to stream.
    /// </summary>
    /// <param name="stringBuilder">String builder instance.</param>
    private sealed class StringBuilderStream(StringBuilder stringBuilder) : Stream
    {
        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotImplementedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            var str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(buffer, offset, count));
            stringBuilder.Append(str);
        }

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => stringBuilder.Length;

        /// <inheritdoc />
        public override long Position { get; set; }
    }

    public TextTableOutput(
        StringBuilder stringBuilder,
        bool hasHeader = true,
        string? separator = null,
        Style style = Style.Table1,
        string? floatNumberFormat = null) : this(
            new StringBuilderStream(stringBuilder),
            hasHeader,
            separator,
            style,
            floatNumberFormat)
    {
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_streamWriter == StreamWriter.Null)
        {
            _streamWriter = new StreamWriter(_stream, encoding: null, bufferSize: -1, leaveOpen: true);
        }
        _logger.LogTrace("Text table opened.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_hasHeader && !_isSingleValue)
        {
            _onFooter.Invoke();
        }
        _streamWriter.Close();
        _logger.LogTrace("Text table closed.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async ValueTask<ErrorCode> OnWriteAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        _onWrite.Invoke(values);
        await _streamWriter.FlushAsync(cancellationToken);
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    protected override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var columns = QueryContext.QueryInfo.Columns;
        _isSingleValue = columns.Length == 1 && columns[0].Name == SingleValueRowsIterator.ColumnTitle;
        _columnsLengths = new int[columns.Length];

        _onHeader.Invoke();
        await _streamWriter.FlushAsync(cancellationToken);
    }

    #region Table1

    private void OnTable1Header()
    {
        var columns = QueryContext.QueryInfo.Columns;
        _totalMaxLineLength = new int[columns.Length];
        int currentMaxLength = 0;

        if (!_hasHeader || _isSingleValue)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                currentMaxLength += _separatorWithSpace.Length + columns[i].Length + 1;
                _totalMaxLineLength[i] = currentMaxLength;
            }
            return;
        }

        // Header.
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _columnsLengths[i] = columns[i].Length;

            _streamWriter.Write(_separatorWithSpace);
            _streamWriter.Write(columns[i].FullName.PadRight(_columnsLengths[i]));
            _streamWriter.Write(' ');
            currentMaxLength += _separatorWithSpace.Length + columns[i].Length + 1;
            _totalMaxLineLength[i] = currentMaxLength;
        }
        if (currentMaxLength > 0)
        {
            _streamWriter.Write(_separator);
        }
        _streamWriter.WriteLine();
        _streamWriter.Flush();

        // Append header separator.
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _columnsLengths[i] = columns[i].Length;
            _streamWriter.Write(_separatorWithSpace);
            _streamWriter.Write(new string('-', _columnsLengths[i]));
            _streamWriter.Write(' ');
        }
        if (currentMaxLength > 0)
        {
            _streamWriter.Write(_separator);
        }
        _streamWriter.WriteLine();
    }

    private void OnTable1Write(VariantValue[] values)
    {
        int writeCount = 0;
        var columns = QueryContext.QueryInfo.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }
            if (!_isSingleValue)
            {
                _streamWriter.Write(_separatorWithSpace);
                writeCount += _separatorWithSpace.Length;
            }
            var value = values[i];
            var valueString = ToStringWithFormat(value);
            var padding = _columnsLengths[i];
            var exceed = _totalMaxLineLength[i] - writeCount - _columnsLengths[i] - 1;
            if (exceed < 0)
            {
                padding = Math.Max(0, exceed + padding);
            }
            var valueWithPadding = valueString.PadRight(padding);
            _streamWriter.Write(valueWithPadding);
            _streamWriter.Write(' ');
            writeCount += valueWithPadding.Length + 1;
        }
        if (!_isSingleValue && writeCount > 0)
        {
            _streamWriter.Write(_separator);
        }
        _streamWriter.WriteLine();
    }

    private void OnTable1Footer()
    {
    }

    #endregion

    #region Table2

    private void OnTable2Header()
    {
        var columns = QueryContext.QueryInfo.Columns;
        _totalMaxLineLength = new int[columns.Length];
        int currentMaxLength = 0;

        if (!_hasHeader || _isSingleValue)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                var separatorLength = 0;
                if (i > 0)
                {
                    _streamWriter.Write(_separatorWithSpace);
                    separatorLength = _separatorWithSpace.Length;
                }
                currentMaxLength += separatorLength + columns[i].Length + 1;
                _totalMaxLineLength[i] = currentMaxLength;
            }
            return;
        }

        // Header.
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _columnsLengths[i] = columns[i].Length;

            var separatorLength = 0;
            if (i > 0)
            {
                _streamWriter.Write(_separatorWithSpace);
                separatorLength = _separatorWithSpace.Length;
            }
            _streamWriter.Write(columns[i].FullName.PadRight(_columnsLengths[i]));
            _streamWriter.Write(' ');
            currentMaxLength += separatorLength + columns[i].Length + 1;
            _totalMaxLineLength[i] = currentMaxLength;
        }
        _streamWriter.WriteLine();
        _streamWriter.Flush();

        // Append header separator.
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _columnsLengths[i] = columns[i].Length;

            if (i > 0)
            {
                _streamWriter.Write("+-");
            }
            _streamWriter.Write(new string('-', _columnsLengths[i] + 1));
        }
        _streamWriter.WriteLine();
    }

    private void OnTable2Write(VariantValue[] values)
    {
        int writeCount = 0;
        var columns = QueryContext.QueryInfo.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }
            if (!_isSingleValue && i > 0)
            {
                _streamWriter.Write(_separatorWithSpace);
                writeCount += _separatorWithSpace.Length;
            }
            var value = values[i];
            var valueString = ToStringWithFormat(value);
            var padding = _columnsLengths[i];
            var exceed = _totalMaxLineLength[i] - writeCount - _columnsLengths[i] - 1;
            if (exceed < 0)
            {
                padding = Math.Max(0, exceed + padding);
            }
            var valueWithPadding = valueString.PadRight(padding);
            _streamWriter.Write(valueWithPadding);
            _streamWriter.Write(' ');
            writeCount += valueWithPadding.Length + 1;
        }
        _streamWriter.WriteLine();
    }

    private void OnTable2Footer()
    {
    }

    #endregion

    #region No Space Table

    private void OnNoSpaceTableHeader()
    {
        var columns = QueryContext.QueryInfo.Columns;
        if (!_hasHeader || _isSingleValue)
        {
            return;
        }

        // Header.
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _streamWriter.Write(_separator);
            _streamWriter.Write(columns[i].FullName);
            _columnsLengths[i] = columns[i].Length;
        }
        _streamWriter.Write(_separator);
        _streamWriter.WriteLine();
        _streamWriter.Flush();
    }

    private void OnNoSpaceTableWrite(VariantValue[] values)
    {
        var columns = QueryContext.QueryInfo.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }
            var valueString = ToStringWithFormat(values[i]);
            _streamWriter.Write(_separator);
            _streamWriter.Write(valueString);
        }
        if (!_isSingleValue)
        {
            _streamWriter.Write(_separator);
        }
        _streamWriter.WriteLine();
    }

    private void OnNoSpaceTableFooter()
    {
    }

    #endregion

    #region Card

    private void OnCardHeader()
    {
        _maxColumnNameWidth = Math.Min(QueryContext.QueryInfo.Columns
            .Where(c => !c.IsHidden)
            .Select(c => c.Name.Length)
            .Max(), 20);
    }

    private void OnCardWrite(VariantValue[] values)
    {
        var columns = QueryContext.QueryInfo.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }
            var valueString = ToStringWithFormat(values[i]);
            _streamWriter.Write(columns[i].FullName.PadLeft(_maxColumnNameWidth));
            _streamWriter.Write($" {_separator} {valueString}\n");
        }
        _streamWriter.WriteLine();
    }

    private void OnCardFooter()
    {
    }

    #endregion

    #region SolidTable

    private void OnSolidTableHeader()
    {
        var columns = QueryContext.QueryInfo.Columns;
        _totalMaxLineLength = new int[columns.Length];
        int currentMaxLength = 0;

        if (!_hasHeader || _isSingleValue)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                var separatorLength = 0;
                if (i > 0)
                {
                    _streamWriter.Write(_separatorWithSpace);
                    separatorLength = _separatorWithSpace.Length;
                }
                currentMaxLength += separatorLength + columns[i].Length + 1;
                _totalMaxLineLength[i] = currentMaxLength;
            }
            return;
        }

        // Append header separator.
        _streamWriter.Write(BoxBottomRight);
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _columnsLengths[i] = columns[i].Length;

            if (i > 0)
            {
                _streamWriter.Write(BoxLeftBottomRight);
            }
            _streamWriter.Write(new string(BoxLeftRight, _columnsLengths[i] + 2));
        }
        _streamWriter.Write(BoxLeftBottom);
        _streamWriter.WriteLine();
        _streamWriter.Flush();

        // Header.
        _streamWriter.Write(_separatorWithSpace);
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _columnsLengths[i] = columns[i].Length;

            var separatorLength = 0;
            if (i > 0)
            {
                _streamWriter.Write(_separatorWithSpace);
                separatorLength = _separatorWithSpace.Length;
            }
            _streamWriter.Write(columns[i].FullName.PadRight(_columnsLengths[i]));
            _streamWriter.Write(' ');
            currentMaxLength += separatorLength + columns[i].Length + 1;
            _totalMaxLineLength[i] = currentMaxLength;
        }
        _streamWriter.Write(_separator);
        _streamWriter.WriteLine();
        _streamWriter.Flush();

        // Append header separator.
        _streamWriter.Write(BoxBottomRightTop);
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _columnsLengths[i] = columns[i].Length;

            if (i > 0)
            {
                _streamWriter.Write(BoxLeftBottomRightTop);
            }
            _streamWriter.Write(new string(BoxLeftRight, _columnsLengths[i] + 2));
        }
        _streamWriter.Write(BoxLeftBottomTop);
        _streamWriter.WriteLine();
    }

    private void OnSolidTableWrite(VariantValue[] values)
    {
        int writeCount = 0;
        var columns = QueryContext.QueryInfo.Columns;
        _streamWriter.Write(_separatorWithSpace);
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }
            if (!_isSingleValue && i > 0)
            {
                _streamWriter.Write(_separatorWithSpace);
                writeCount += _separatorWithSpace.Length;
            }
            var value = values[i];
            var valueString = ToStringWithFormat(value);
            var padding = _columnsLengths[i];
            var exceed = _totalMaxLineLength[i] - writeCount - _columnsLengths[i] - 1;
            if (exceed < 0)
            {
                padding = Math.Max(0, exceed + padding);
            }
            var valueWithPadding = valueString.PadRight(padding);
            _streamWriter.Write(valueWithPadding);
            _streamWriter.Write(' ');
            writeCount += valueWithPadding.Length + 1;
        }
        _streamWriter.Write(_separatorWithSpace);
        _streamWriter.WriteLine();
    }

    private void OnSolidTableFooter()
    {
        var columns = QueryContext.QueryInfo.Columns;

        _streamWriter.Write(BoxRightTop);
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _columnsLengths[i] = columns[i].Length;

            if (i > 0)
            {
                _streamWriter.Write(BoxLeftTopRight);
            }
            _streamWriter.Write(new string(BoxLeftRight, _columnsLengths[i] + 2));
        }
        _streamWriter.Write(BoxLeftTop);
        _streamWriter.WriteLine();
        _streamWriter.Flush();
    }

    #endregion

    private string ToStringWithFormat(in VariantValue value)
    {
        var type = value.Type;
        if (type == DataType.Float || type == DataType.Numeric)
        {
            return value.ToString(_floatNumberFormat);
        }
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _streamWriter.Dispose();
        _stream.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        await _streamWriter.DisposeAsync();
        await _stream.DisposeAsync();
    }
}
