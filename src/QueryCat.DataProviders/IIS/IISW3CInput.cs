using System.Globalization;
using QueryCat.Backend;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.DataProviders.IIS;

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

    // https://procodeguide.com/programming/iis-logs/.
    private static readonly Dictionary<string, Column> AvailableFields = new()
    {
        ["date"] = new("date", DataType.Timestamp, "Date of request"),
        ["time"] = new("time", DataType.String, "Time of request in UTC"),
        ["c-ip"] = new("c-ip", DataType.String, "The client IP address that made the request"),
        ["cs-username"] = new("cs-username", DataType.String, "The name of the authenticated user who made the request"),
        ["s-sitename"] = new("s-sitename", DataType.String, "The site service name and instance number that handled the request"),
        ["s-computername"] = new("s-computername", DataType.String, "The name of the server on which request was made"),
        ["s-ip"] = new("s-ip", DataType.String, "The IP address of the server on which request was made"),
        ["s-port"] = new("s-port", DataType.String, "The server port number that is configured for the service"),
        ["cs-method"] = new("cs-method", DataType.String, "The requested action, for example, a GET method"),
        ["cs-uri-stem"] = new("cs-uri-stem", DataType.String, "The URI, or target, of the action"),
        ["cs-uri-query"] = new("cs-uri-query", DataType.String, "The query, if any, that the client was trying to perform"),
        ["sc-status"] = new("sc-status", DataType.String, "The HTTP request status code"),
        ["sc-substatus"] = new("sc-substatus", DataType.String, "The HTTP request substatus error code"),
        ["sc-win32-status"] = new("sc-win32-status", DataType.String, "The Windows status code"),
        ["sc-bytes"] = new("sc-bytes", DataType.Numeric, "The number of bytes that the server sent to the client"),
        ["cs-bytes"] = new("cs-bytes", DataType.Integer, "The number of bytes that the server received from the client"),
        ["time-taken"] = new("time-taken", DataType.Integer, "The time that the request took to complete (in milliseconds)"),
        ["cs-version"] = new("cs-version", DataType.String, "The HTTP protocol version that the client used"),
        ["cs-host"] = new("cs-host", DataType.String, "The hostname, if any"),
        ["cs(User-Agent)"] = new("cs(User-Agent)", DataType.String, "The browser type that client used for request"),
        ["cs(Cookie)"] = new("cs(Cookie)", DataType.String, "The content of the cookie sent or received"),
        ["cs(Referer)"] = new("cs(Referer)", DataType.String, "The site that the user last visited"),
    };

    public IISW3CInput(Stream stream) : base(new StreamReader(stream), new DelimiterStreamReader.ReaderOptions
    {
        Delimiters = new[] { ' ' }
    })
    {
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (columnIndex == _dateColumnIndex)
        {
            if (_timeColumnIndex == -1)
            {
                value = new VariantValue(DateTime
                    .ParseExact(GetColumnValue(_dateColumnIndex), "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture));
            }
            else
            {
                var stringDate = string.Concat(GetColumnValue(_dateColumnIndex), " ", GetColumnValue(_timeColumnIndex));
                value = new VariantValue(DateTime
                    .ParseExact(stringDate, "yyyy'-'MM'-'dd HH:mm:ss", CultureInfo.InvariantCulture));
            }
        }
        var offset = _dateColumnIndex > -1 && columnIndex > _dateColumnIndex ? 1 : 0;
        return base.ReadValue(columnIndex + offset, out value);
    }

    /// <inheritdoc />
    protected override bool IgnoreLine()
    {
        if (Columns.Length < 1)
        {
            return false;
        }
        var row = GetRowText();
        return row.Slice(0, 1).FirstSpan[0] == '#';
    }

    /// <inheritdoc />
    public override void Open()
    {
        // Try to find fields header.
        bool foundHeaders = false;
        while (ReadNext())
        {
            var line = GetColumnValue(0);
            if (line.StartsWith(FieldsMarker))
            {
                ParseHeaders(GetRowText().ToString());
                foundHeaders = true;
                break;
            }
        }

        if (!foundHeaders)
        {
            throw new QueryCatException("Cannot find IIS fields.");
        }
    }

    public void ParseHeaders(ReadOnlySpan<char> header)
    {
        var fields = header.ToString().Substring(FieldsMarker.Length)
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var columns = new List<Column>();
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (AvailableFields.TryGetValue(field, out Column? column))
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
        }

        SetColumns(columns);
    }
}
