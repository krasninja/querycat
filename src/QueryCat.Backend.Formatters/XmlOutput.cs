using System.Xml;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

internal sealed class XmlOutput : RowsOutput, IDisposable
{
    private const string RootTagName = "FRAME";
    private const string RowTagName = "ROW";

    private readonly XmlWriter _xmlWriter;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<XmlOutput>();

    public XmlOutput(Stream stream)
    {
        _xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Async = false,
            Indent = true,
        });
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
        _xmlWriter.WriteStartDocument();
        _xmlWriter.WriteStartElement(RootTagName);
    }

    /// <inheritdoc />
    public override void Open()
    {
        _logger.LogTrace("XML opened.");
    }

    /// <inheritdoc />
    public override void Close()
    {
        _xmlWriter.WriteEndElement(); // RootTagName.
        _xmlWriter.WriteEndDocument();
        _xmlWriter.Flush();
        _xmlWriter.Close();
        _logger.LogTrace("XML closed.");
    }

    /// <inheritdoc />
    protected override void OnWrite(in VariantValue[] values)
    {
        _xmlWriter.WriteStartElement(RowTagName);
        var columns = QueryContext.QueryInfo.Columns;
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].IsHidden)
            {
                continue;
            }
            _xmlWriter.WriteStartElement(columns[i].Name);
            switch (columns[i].DataType)
            {
                case DataType.Boolean:
                    _xmlWriter.WriteValue(values[i].AsBoolean);
                    break;
                case DataType.Float:
                    _xmlWriter.WriteValue(values[i].AsFloat);
                    break;
                case DataType.Integer:
                    _xmlWriter.WriteValue(values[i].AsInteger);
                    break;
                case DataType.Numeric:
                    _xmlWriter.WriteValue(values[i].AsNumeric);
                    break;
                case DataType.String:
                    _xmlWriter.WriteValue(values[i].AsString);
                    break;
                case DataType.Timestamp:
                    _xmlWriter.WriteValue(values[i].AsTimestamp);
                    break;
                default:
                    _xmlWriter.WriteValue(values[i].ToString());
                    break;
            }
            _xmlWriter.WriteEndElement();
        }
        _xmlWriter.WriteEndElement(); // RowTagName.
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Close();
        _xmlWriter.Dispose();
    }
}
