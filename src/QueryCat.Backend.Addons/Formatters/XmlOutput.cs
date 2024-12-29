using System.Globalization;
using System.Xml;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Addons.Formatters;

internal sealed class XmlOutput : RowsOutput, IDisposable
{
    private const string RootTagName = "FRAME";
    private const string RowTagName = "ROW";

    private readonly XmlWriter _xmlWriter;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(XmlOutput));

    public XmlOutput(Stream stream)
    {
        _xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Async = false,
            Indent = true,
        });
    }

    /// <inheritdoc />
    protected override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _xmlWriter.WriteStartDocumentAsync();
        _xmlWriter.WriteStartElement(RootTagName);
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("XML opened.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await _xmlWriter.WriteEndElementAsync(); // RootTagName.
        await _xmlWriter.WriteEndDocumentAsync();
        await _xmlWriter.FlushAsync();
        _xmlWriter.Close();
        _logger.LogTrace("XML closed.");
    }

    /// <inheritdoc />
    protected override void OnWrite(in VariantValue[] values)
    {
        _xmlWriter.WriteStartElement(RowTagName);
        var columns = QueryContext.QueryInfo.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }
            _xmlWriter.WriteStartElement(columns[i].Name);
            switch (columns[i].DataType)
            {
                case DataType.Boolean:
                    _xmlWriter.WriteValue(values[i].AsBooleanUnsafe);
                    break;
                case DataType.Float:
                    _xmlWriter.WriteValue(values[i].AsFloatUnsafe);
                    break;
                case DataType.Integer:
                    _xmlWriter.WriteValue(values[i].AsIntegerUnsafe);
                    break;
                case DataType.Numeric:
                    _xmlWriter.WriteValue(values[i].AsNumericUnsafe);
                    break;
                case DataType.String:
                    _xmlWriter.WriteValue(values[i].AsStringUnsafe);
                    break;
                case DataType.Timestamp:
                    _xmlWriter.WriteValue(values[i].AsTimestampUnsafe);
                    break;
                default:
                    _xmlWriter.WriteValue(values[i].ToString(CultureInfo.InvariantCulture));
                    break;
            }
            _xmlWriter.WriteEndElement();
        }
        _xmlWriter.WriteEndElement(); // RowTagName.
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AsyncUtils.RunSync(CloseAsync);
        _xmlWriter.Dispose();
    }
}
