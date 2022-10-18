using System.Text;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Delimiter separated values (DSV) output.
/// </summary>
public sealed class DsvOutput : RowsOutput, IDisposable
{
    private readonly StreamWriter _streamWriter;
    private readonly char _delimiter;
    private readonly bool _hasHeader;
    private bool _wroteHeader;

    public DsvOutput(Stream stream, char delimiter, bool hasHeader = true)
    {
        _streamWriter = new StreamWriter(stream, Encoding.Default, -1, leaveOpen: true);
        _delimiter = delimiter;
        _hasHeader = hasHeader;
    }

    /// <inheritdoc />
    public override void Open()
    {
        Logger.Instance.Trace("Opened.", nameof(DsvOutput));
    }

    /// <inheritdoc />
    public override void Close()
    {
        _streamWriter.Close();
        Logger.Instance.Trace("Closed.", nameof(DsvOutput));
    }

    /// <inheritdoc />
    protected override void OnWrite(Row row)
    {
        var columns = row.Columns;
        var length = columns.Length;
        for (int i = 0; i < length; i++)
        {
            if (!row[i].IsNull)
            {
                switch (columns[i].DataType)
                {
                    case DataType.Integer:
                        _streamWriter.Write(row[i].AsInteger);
                        break;
                    case DataType.Float:
                        _streamWriter.Write(row[i].AsFloat);
                        break;
                    case DataType.Numeric:
                        _streamWriter.Write(row[i].AsNumeric);
                        break;
                    case DataType.String:
                        WriteString(row[i].AsString);
                        break;
                    case DataType.Timestamp:
                        _streamWriter.Write(row[i].AsTimestamp);
                        break;
                    case DataType.Interval:
                        _streamWriter.Write(row[i].AsInterval);
                        break;
                    case DataType.Boolean:
                        _streamWriter.Write(row[i].AsBoolean);
                        break;
                }
            }
            if (i < length - 1)
            {
                _streamWriter.Write(_delimiter);
            }
        }
        _streamWriter.WriteLine();
        _streamWriter.Flush();
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
        WriteHeader(QueryContext);
    }

    private void WriteHeader(QueryContext queryContext)
    {
        var columns = queryContext.GetColumns().ToArray();

        if (_hasHeader && !_wroteHeader)
        {
            var length = columns.Length;
            for (int i = 0; i < length; i++)
            {
                WriteString(columns[i].Name);
                if (i < length - 1)
                {
                    _streamWriter.Write(_delimiter);
                }
            }
            _streamWriter.WriteLine();
            _streamWriter.Flush();
            _wroteHeader = true;
        }
    }

    private void WriteString(string str)
    {
        var containsDelimiter = str.IndexOf(_delimiter) > -1;
        if (containsDelimiter)
        {
            _streamWriter.Write('\"');
            _streamWriter.Write(str.Replace("\"", "\"\""));
            _streamWriter.Write('\"');
        }
        else
        {
            _streamWriter.Write(str);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _streamWriter.Dispose();
    }
}
