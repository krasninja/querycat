using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Delimiter separated values (DSV) output.
/// </summary>
internal sealed class DsvOutput : RowsOutput, IDisposable
{
    private const char DefaultDelimiter = ',';

    private readonly StreamWriter _streamWriter;
    private readonly char _delimiter;
    private readonly bool _hasHeader;
    private readonly bool _quoteStrings;
    private bool _wroteHeader;

    internal Stream Stream { get; }

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DsvOutput));

    public DsvOutput(DsvOptions dsvOptions)
    {
        Stream = dsvOptions.Stream;
        _streamWriter = new StreamWriter(Stream, Encoding.Default, -1, leaveOpen: true);
        _delimiter = dsvOptions.InputOptions.DelimiterStreamReaderOptions.Delimiters.Length > 0 ?
            dsvOptions.InputOptions.DelimiterStreamReaderOptions.Delimiters[0]
            : DefaultDelimiter;
        _hasHeader = dsvOptions.HasHeader ?? true;
        _quoteStrings = dsvOptions.QuoteStrings;
    }

    /// <inheritdoc />
    public override void Open()
    {
        _logger.LogTrace("DSV opened.");
    }

    /// <inheritdoc />
    public override void Close()
    {
        _streamWriter.Close();
        _logger.LogTrace("DSV closed.");
    }

    /// <inheritdoc />
    protected override void OnWrite(in VariantValue[] values)
    {
        var columns = QueryContext.QueryInfo.Columns;
        var length = columns.Count;
        for (int i = 0; i < length; i++)
        {
            if (!values[i].IsNull)
            {
                switch (columns[i].DataType)
                {
                    case DataType.Integer:
                        _streamWriter.Write(values[i].AsInteger);
                        break;
                    case DataType.Float:
                        _streamWriter.Write(values[i].AsFloat);
                        break;
                    case DataType.Numeric:
                        _streamWriter.Write(values[i].AsNumeric);
                        break;
                    case DataType.String:
                        WriteString(values[i].AsString);
                        break;
                    case DataType.Timestamp:
                        _streamWriter.Write(values[i].AsTimestamp);
                        break;
                    case DataType.Interval:
                        _streamWriter.Write(values[i].AsInterval);
                        break;
                    case DataType.Boolean:
                        _streamWriter.Write(values[i].AsBoolean);
                        break;
                    case DataType.Blob:
                        values[i].AsBlob.ApplyAction(new ReadOnlySpanAction<byte, object?>((span, o) =>
                        {
                            _streamWriter.Write(Convert.ToHexString(span));
                        }), 0);
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
        var columns = queryContext.QueryInfo.Columns;

        if (_hasHeader && !_wroteHeader)
        {
            var length = columns.Count;
            for (var i = 0; i < length; i++)
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
        var containsDelimiter = _quoteStrings || str.IndexOf(_delimiter) > -1
            || str.Contains('\n');
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
