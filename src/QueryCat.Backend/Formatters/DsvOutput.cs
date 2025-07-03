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
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("DSV opened.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _streamWriter.Close();
        _logger.LogTrace("DSV closed.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async ValueTask<ErrorCode> OnWriteAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        var columns = QueryContext.QueryInfo.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            if (!values[i].IsNull)
            {
                var type = columns[i].DataType;
                // Try to get more precise type if it is object.
                if (type == DataType.Dynamic)
                {
                    type = values[i].Type;
                }
                switch (type)
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
                        {
                            await using var stream = values[i].AsBlobUnsafe.GetStream();
                            var arr = ArrayPool<byte>.Shared.Rent(4096);
                            int bytesRead;
                            while ((bytesRead = stream.Read(arr)) > 0)
                            {
                                _streamWriter.Write(Convert.ToHexString(arr, 0, bytesRead));
                            }
                            ArrayPool<byte>.Shared.Return(arr);
                        }
                        break;
                }
            }
            if (i < columns.Length - 1)
            {
                _streamWriter.Write(_delimiter);
            }
        }
        _streamWriter.WriteLine();
        await _streamWriter.FlushAsync(cancellationToken);

        return ErrorCode.OK;
    }

    /// <inheritdoc />
    protected override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return WriteHeaderAsync(QueryContext, cancellationToken);
    }

    private async Task WriteHeaderAsync(QueryContext queryContext, CancellationToken cancellationToken)
    {
        var columns = queryContext.QueryInfo.Columns;

        if (_hasHeader && !_wroteHeader)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                WriteString(columns[i].Name);
                if (i < columns.Length - 1)
                {
                    await _streamWriter.WriteAsync(_delimiter);
                }
            }
            await _streamWriter.WriteLineAsync();
            await _streamWriter.FlushAsync(cancellationToken);
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
