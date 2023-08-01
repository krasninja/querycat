using Microsoft.Extensions.Logging;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Output rows into text writer. The result will be formatted
/// using spaces.
/// </summary>
public sealed class TextTableOutput : RowsOutput, IDisposable
{
    /// <summary>
    /// Table style.
    /// </summary>
    public enum Style
    {
        Table,
        NoSpaceTable,
        Card,
    }

    private readonly Stream _stream;
    private int[] _totalMaxLineLength = Array.Empty<int>();
    private StreamWriter _streamWriter = StreamWriter.Null;
    private bool _isSingleValue;
    private int _maxColumnNameWidth = 10;

    private readonly bool _hasHeader;
    private readonly string _separator;
    private readonly string _separatorWithSpace;
    private readonly string _floatNumberFormat;

    private readonly Action _onInit;
    private readonly Action<Row> _onWrite;

    private int[] _columnsLengths = Array.Empty<int>();

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<TextTableOutput>();

    /// <summary>
    /// Columns separator.
    /// </summary>
    public string Separator => _separator;

    public TextTableOutput(
        Stream stream,
        bool hasHeader = true,
        string? separator = null,
        Style style = Style.Table,
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
            _onInit = OnCardInit;
            _onWrite = OnCardWrite;
            _separator = separator ?? ":";
        }
        else if (style == Style.Table)
        {
            _onInit = OnTableInit;
            _onWrite = OnTableWrite;
            _separator = separator ?? "|";
        }
        else if (style == Style.NoSpaceTable)
        {
            _onInit = OnNoSpaceTableInit;
            _onWrite = OnNoSpaceTableWrite;
            _separator = separator ?? "|";
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(style));
        }

        _separatorWithSpace = !string.IsNullOrEmpty(_separator) ? _separator + " " : string.Empty;
    }

    /// <inheritdoc />
    public override void Open()
    {
        if (_streamWriter == StreamWriter.Null)
        {
            _streamWriter = new StreamWriter(_stream, encoding: null, bufferSize: -1, leaveOpen: true);
        }
        _logger.LogTrace("Text table opened.");
    }

    /// <inheritdoc />
    public override void Close()
    {
        _streamWriter.Close();
        _logger.LogTrace("Text table closed.");
    }

    /// <inheritdoc />
    protected override void OnWrite(Row row)
    {
        _onWrite.Invoke(row);
        _streamWriter.Flush();
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
        var columns = QueryContext.QueryInfo.Columns;
        _isSingleValue = columns.Count == 1 && columns[0].Name == SingleValueRowsIterator.ColumnTitle;
        _columnsLengths = new int[columns.Count];

        _onInit.Invoke();
        _streamWriter.Flush();
    }

    #region Table

    private void OnTableInit()
    {
        var columns = QueryContext.QueryInfo.Columns;
        _totalMaxLineLength = new int[columns.Count];
        int currentMaxLength = 0;

        if (!_hasHeader || _isSingleValue)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                currentMaxLength += _separatorWithSpace.Length + columns[i].Length + 1;
                _totalMaxLineLength[i] = currentMaxLength;
            }
            return;
        }

        // Header.
        for (int i = 0; i < columns.Count; i++)
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
        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            var lengths = new[]
            {
                columns[i].Length
            };
            _columnsLengths[i] = lengths.Max();

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

    private void OnTableWrite(Row row)
    {
        int writeCount = 0;
        for (int i = 0; i < row.Columns.Length; i++)
        {
            if (row.Columns[i].IsHidden)
            {
                continue;
            }
            if (!_isSingleValue)
            {
                _streamWriter.Write(_separatorWithSpace);
                writeCount += _separatorWithSpace.Length;
            }
            var value = row[i];
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

    #endregion

    #region No Space Table

    private void OnNoSpaceTableInit()
    {
        var columns = QueryContext.QueryInfo.Columns;
        if (!_hasHeader || _isSingleValue)
        {
            return;
        }

        // Header.
        for (int i = 0; i < columns.Count; i++)
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

    private void OnNoSpaceTableWrite(Row row)
    {
        for (int i = 0; i < row.Columns.Length; i++)
        {
            if (row.Columns[i].IsHidden)
            {
                continue;
            }
            var valueString = ToStringWithFormat(row[i]);
            _streamWriter.Write(_separator);
            _streamWriter.Write(valueString);
        }
        if (!_isSingleValue)
        {
            _streamWriter.Write(_separator);
        }
        _streamWriter.WriteLine();
    }

    #endregion

    #region Card

    private void OnCardInit()
    {
        _maxColumnNameWidth = Math.Min(QueryContext.QueryInfo.Columns
            .Where(c => !c.IsHidden)
            .Select(c => c.Name.Length)
            .Max(), 20);
    }

    private void OnCardWrite(Row row)
    {
        for (int i = 0; i < row.Columns.Length; i++)
        {
            if (row.Columns[i].IsHidden)
            {
                continue;
            }
            var valueString = ToStringWithFormat(row[i]);
            _streamWriter.Write(row.Columns[i].FullName.PadLeft(_maxColumnNameWidth));
            _streamWriter.Write($" {_separator} {valueString}\n");
        }
        _streamWriter.WriteLine();
    }

    #endregion

    private string ToStringWithFormat(in VariantValue value)
    {
        var type = value.GetInternalType();
        if (type == DataType.Float || type == DataType.Numeric)
        {
            return value.ToString(_floatNumberFormat);
        }
        return value.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Close();
        _stream.Dispose();
    }
}
