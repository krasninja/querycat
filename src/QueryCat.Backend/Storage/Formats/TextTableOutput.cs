using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Storage.Formats;

/// <summary>
/// Output rows into text writer. The result will be formatted
/// using spaces.
/// </summary>
public sealed class TextTableOutput : RowsOutput, IDisposable
{
    public const string Separator = "|";

    private readonly Stream _stream;
    private StreamWriter? _streamWriter;
    private bool _isSingleValue;

    private readonly bool _hasHeader;

    private int[] _columnsLengths = Array.Empty<int>();

    public TextTableOutput(Stream stream, bool hasHeader = true)
    {
        _stream = stream;
        _hasHeader = hasHeader;
    }

    /// <inheritdoc />
    public override void Open()
    {
        if (_streamWriter == null)
        {
            _streamWriter = new StreamWriter(_stream, encoding: null, bufferSize: -1, leaveOpen: true);
        }
        Logger.Instance.Trace("Opened.", nameof(DsvOutput));
    }

    /// <inheritdoc />
    public override void Close()
    {
        if (_streamWriter != null)
        {
            _streamWriter.Close();
            _streamWriter = null;
        }
        Logger.Instance.Trace("Closed.", nameof(DsvOutput));
    }

    /// <inheritdoc />
    protected override void OnWrite(Row row)
    {
        if (_streamWriter == null)
        {
            return;
        }

        for (int i = 0; i < row.Iterator.Columns.Length; i++)
        {
            if (!_isSingleValue)
            {
                _streamWriter.Write(Separator + " ");
            }
            var value = row[i];
            _streamWriter.Write(value.ToString().PadRight(_columnsLengths[i]));
            _streamWriter.Write(' ');
        }
        if (!_isSingleValue)
        {
            _streamWriter.Write(Separator);
        }
        _streamWriter.WriteLine();
        _streamWriter.Flush();
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
        if (_streamWriter == null)
        {
            return;
        }

        var columns = QueryContext.GetColumns().ToArray();
        _isSingleValue = columns.Length == 1 && columns[0].Name == SingleValueRowsIterator.ColumnTitle;
        _columnsLengths = new int[columns.Length];
        if (!_hasHeader || _isSingleValue)
        {
            return;
        }

        for (int i = 0; i < columns.Length; i++)
        {
            var lengths = new[]
            {
                columns[i].Length
            };
            _columnsLengths[i] = lengths.Max();

            _streamWriter.Write(Separator + " ");
            _streamWriter.Write(columns[i].Name.PadRight(_columnsLengths[i]));
            _streamWriter.Write(' ');
        }
        _streamWriter.Write(Separator);
        _streamWriter.WriteLine();
        _streamWriter.Flush();

        for (int i = 0; i < columns.Length; i++)
        {
            var lengths = new[]
            {
                columns[i].Length
            };
            _columnsLengths[i] = lengths.Max();

            _streamWriter.Write(Separator + " ");
            _streamWriter.Write(new string('-', _columnsLengths[i]));
            _streamWriter.Write(' ');
        }
        _streamWriter.Write(Separator);
        _streamWriter.WriteLine();
        _streamWriter.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Close();
        _stream.Dispose();
    }
}
