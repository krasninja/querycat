using System.Xml;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Relational;
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
    protected override void OnWrite(Row row)
    {
        _xmlWriter.WriteStartElement(RowTagName);
        for (var i = 0; i < row.Columns.Length; i++)
        {
            if (row.Columns[i].IsHidden)
            {
                continue;
            }
            _xmlWriter.WriteStartElement(row.Columns[i].Name);
            switch (row.Columns[i].DataType)
            {
                case DataType.Boolean:
                    _xmlWriter.WriteValue(row[i].AsBoolean);
                    break;
                case DataType.Float:
                    _xmlWriter.WriteValue(row[i].AsFloat);
                    break;
                case DataType.Integer:
                    _xmlWriter.WriteValue(row[i].AsInteger);
                    break;
                case DataType.Numeric:
                    _xmlWriter.WriteValue(row[i].AsNumeric);
                    break;
                case DataType.String:
                    _xmlWriter.WriteValue(row[i].AsString);
                    break;
                case DataType.Timestamp:
                    _xmlWriter.WriteValue(row[i].AsTimestamp);
                    break;
                default:
                    _xmlWriter.WriteValue(row[i].ToString());
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
