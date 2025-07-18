using System.Collections.Frozen;
using System.Globalization;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// Input for IIS W3C logs.
/// </summary>
// ReSharper disable once IdentifierTypo
// ReSharper disable once InconsistentNaming
public sealed class IISW3CInput : StreamRowsInput
{
    // https://docs.microsoft.com/en-us/previous-versions/iis/6.0-sdk/ms525807(v=vs.90).
    private const string FieldsMarker = "#Fields:";

    private int _timeColumnIndex = -1;
    private int _dateColumnIndex = -1;
    private int _dataStartRowIndex = -1;
    private bool _isInitialized;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(IISW3CInput));

    // https://procodeguide.com/programming/iis-logs/.
    private static readonly FrozenDictionary<string, Column> _availableFields = new Dictionary<string, Column>
    {
        ["date"] = new("date", DataType.Timestamp, "Date of request."),
        ["time"] = new("time", DataType.String, "Time of request in UTC."),
        ["c-ip"] = new("c-ip", DataType.String, "The client IP address that made the request."),
        ["cs-username"] = new("cs-username", DataType.String, "The name of the authenticated user who made the request."),
        ["s-sitename"] = new("s-sitename", DataType.String, "The site service name and instance number that handled the request."),
        ["s-computername"] = new("s-computername", DataType.String, "The name of the server on which request was made."),
        ["s-ip"] = new("s-ip", DataType.String, "The IP address of the server on which request was made."),
        ["s-port"] = new("s-port", DataType.Integer, "The server port number that is configured for the service."),
        ["cs-method"] = new("cs-method", DataType.String, "The requested action, for example, a GET method."),
        ["cs-uri-stem"] = new("cs-uri-stem", DataType.String, "The URI, or target, of the action."),
        ["cs-uri-query"] = new("cs-uri-query", DataType.String, "The query, if any, that the client was trying to perform."),
        ["sc-status"] = new("sc-status", DataType.String, "The HTTP request status code."),
        ["sc-substatus"] = new("sc-substatus", DataType.String, "The HTTP request substatus error code."),
        ["sc-win32-status"] = new("sc-win32-status", DataType.String, "The Windows status code."),
        ["sc-bytes"] = new("sc-bytes", DataType.Numeric, "The number of bytes that the server sent to the client."),
        ["cs-bytes"] = new("cs-bytes", DataType.Integer, "The number of bytes that the server received from the client."),
        ["time-taken"] = new("time-taken", DataType.Integer, "The time that the request took to complete (in milliseconds)."),
        ["cs-version"] = new("cs-version", DataType.String, "The HTTP protocol version that the client used."),
        ["cs-host"] = new("cs-host", DataType.String, "The hostname, if any."),
        ["cs(User-Agent)"] = new("cs(User-Agent)", DataType.String, "The browser type that client used for request."),
        ["cs(Cookie)"] = new("cs(Cookie)", DataType.String, "The content of the cookie sent or received."),
        ["cs(Referer)"] = new("cs(Referer)", DataType.String, "The site that the user last visited."),
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, Column>
        .AlternateLookup<ReadOnlySpan<char>> _availableFieldsLookup =
            _availableFields.GetAlternateLookup<ReadOnlySpan<char>>();

    public IISW3CInput(Stream stream, string? key = null) : base(stream, new StreamRowsInputOptions
    {
        DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
        {
            Delimiters = [' '],
        }
    }, key ?? string.Empty)
    {
    }

    /// <inheritdoc />
    protected override ErrorCode ReadValueInternal(int nonVirtualColumnIndex, DataType type, out VariantValue value)
    {
        if (nonVirtualColumnIndex == _dateColumnIndex)
        {
            value = VariantValue.Null;
            if (_timeColumnIndex == -1)
            {
                if (DateTime.TryParseExact(GetInputColumnValue(_dateColumnIndex), "yyyy'-'MM'-'dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    value = new VariantValue(date);
                    return ErrorCode.OK;
                }
                return ErrorCode.CannotCast;
            }
            else
            {
                var stringDate = string.Concat(GetInputColumnValue(_dateColumnIndex), " ", GetInputColumnValue(_timeColumnIndex));
                if (DateTime.TryParseExact(stringDate, "yyyy'-'MM'-'dd HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var datetime))
                {
                    value = new VariantValue(datetime);
                    return ErrorCode.OK;
                }
                return ErrorCode.CannotCast;
            }
        }

        var offset = _timeColumnIndex > -1 && _dateColumnIndex > -1 && nonVirtualColumnIndex >= _timeColumnIndex ? 1 : 0;
        var columnValue = GetInputColumnValue(nonVirtualColumnIndex + offset);
        var errorCode = VariantValue.TryCreateFromString(
            columnValue,
            type,
            out value)
            ? ErrorCode.OK : ErrorCode.CannotCast;
        return errorCode;
    }

    /// <inheritdoc />
    protected override bool IgnoreLine()
    {
        if (!_isInitialized)
        {
            return false;
        }
        var row = GetRowText();
        return row.Length == 0 || row.Slice(0, 1).FirstSpan[0] == '#';
    }

    /// <inheritdoc />
    protected override async Task<Column[]> InitializeColumnsAsync(IRowsInput input, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Open {Input}.", this);

        // Try to find fields header.
        var headers = Array.Empty<Column>();
        while (await input.ReadNextAsync(cancellationToken))
        {
            _dataStartRowIndex++;
            var line = GetInputColumnValue(0);
            if (line.StartsWith(FieldsMarker))
            {
                headers = ParseHeaders(GetRowText().ToString()).ToArray();
                _logger.LogDebug("Found headers.");
                break;
            }
        }

        if (headers.Length < 1)
        {
            throw new QueryCatException("Cannot find IIS fields.");
        }

        return headers;
    }

    /// <inheritdoc />
    protected override async Task InitializeHeadDataAsync(CacheRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < _dataStartRowIndex; i++)
        {
            await iterator.MoveNextAsync(cancellationToken);
        }
        iterator.RemoveFirst(_dataStartRowIndex);
    }

    /// <inheritdoc />
    protected override Task InitializeColumnsTypesAsync(IRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task InitializeCompleteAsync(CacheRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        _isInitialized = true;
        return base.InitializeCompleteAsync(iterator, cancellationToken);
    }

    private async ValueTask<bool> SeekToFieldsHeaderAsync(CancellationToken cancellationToken = default)
    {
        while (await ReadNextAsync(cancellationToken))
        {
            var line = GetInputColumnValue(0);
            if (line.StartsWith(FieldsMarker))
            {
                ParseHeaders(GetRowText().ToString());
                _isInitialized = true;
                _logger.LogDebug("Found headers.");
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _isInitialized = false;
        await base.ResetAsync(cancellationToken);
        await SeekToFieldsHeaderAsync(cancellationToken);
    }

    private List<Column> ParseHeaders(ReadOnlySpan<char> header)
    {
        var subheader = header[FieldsMarker.Length..];
        var fieldsRanges = subheader.Split(' ');
        var columns = new List<Column>();
        var i = 0;
        foreach (var fieldRange in fieldsRanges)
        {
            ReadOnlySpan<char> field = subheader[fieldRange];
            if (field.Length < 1)
            {
                continue;
            }
            if (_availableFieldsLookup.TryGetValue(field, out var column))
            {
                if (Column.NameEquals(column, "time"))
                {
                    _timeColumnIndex = i;
                    continue;
                }
                if (Column.NameEquals(column, "date"))
                {
                    _dateColumnIndex = i;
                }
                columns.Add(column);
            }
            else
            {
                columns.Add(new Column(i, DataType.String));
            }
            i++;
        }

        return columns;
    }
}
