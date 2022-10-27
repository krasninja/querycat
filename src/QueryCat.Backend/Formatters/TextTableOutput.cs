using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

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
        Card
    }

    private readonly Stream _stream;
    private StreamWriter _streamWriter = StreamWriter.Null;
    private bool _isSingleValue;
    private int _maxColumnNameWidth = 10;

    private readonly bool _hasHeader;
    private readonly string _separator;
    private readonly string _separatorWithSpace;

    private readonly Action _onInit;
    private readonly Action<Row> _onWrite;

    private int[] _columnsLengths = Array.Empty<int>();

    public TextTableOutput(Stream stream, bool hasHeader = true, string separator = "",
        Style style = Style.Table)
    {
        _stream = stream;
        _hasHeader = hasHeader;

        if (style == Style.Card)
        {
            _onInit = OnCardInit;
            _onWrite = OnCardWrite;
            _separator = !string.IsNullOrEmpty(separator) ? separator : ":";
        }
        else
        {
            _onInit = OnTableInit;
            _onWrite = OnTableWrite;
            _separator = !string.IsNullOrEmpty(separator) ? separator : "|";
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
        Logger.Instance.Trace("Opened.", nameof(TextTableOutput));
    }

    /// <inheritdoc />
    public override void Close()
    {
        _streamWriter.Close();
        Logger.Instance.Trace("Closed.", nameof(TextTableOutput));
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
        var columns = QueryContext.GetColumns().ToArray();
        _isSingleValue = columns.Length == 1 && columns[0].Name == SingleValueRowsIterator.ColumnTitle;
        _columnsLengths = new int[columns.Length];

        _onInit.Invoke();
        _streamWriter.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Close();
        _stream.Dispose();
    }

    #region Table

    private void OnTableInit()
    {
        if (!_hasHeader || _isSingleValue)
        {
            return;
        }

        var columns = QueryContext.GetColumns().ToArray();

        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }

            _columnsLengths[i] = columns[i].Length;

            _streamWriter.Write(_separatorWithSpace);
            _streamWriter.Write(columns[i].FullName.PadRight(_columnsLengths[i]));
            _streamWriter.Write(' ');
        }
        _streamWriter.Write(_separator);
        _streamWriter.WriteLine();
        _streamWriter.Flush();

        for (int i = 0; i < columns.Length; i++)
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
        _streamWriter.Write(_separator);
        _streamWriter.WriteLine();
    }

    private void OnTableWrite(Row row)
    {
        for (int i = 0; i < row.Columns.Length; i++)
        {
            if (row.Columns[i].IsHidden)
            {
                continue;
            }
            if (!_isSingleValue)
            {
                _streamWriter.Write(_separatorWithSpace);
            }
            var value = row[i];
            _streamWriter.Write(value.ToString().PadRight(_columnsLengths[i]));
            _streamWriter.Write(' ');
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
        _maxColumnNameWidth = Math.Min(
            QueryContext.GetColumns()
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
            var value = row[i];
            _streamWriter.Write(row.Columns[i].FullName.PadLeft(_maxColumnNameWidth));
            _streamWriter.Write($" {_separator} {value}\n");
        }
        _streamWriter.WriteLine();
    }

    #endregion
}
