using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Addons.Formatters;

internal sealed class XmlOutput : RowsOutput, IDisposable, IAsyncDisposable
{
    private const string RootTagName = "FRAME";
    private const string RowTagName = "ROW";
    private const string ItemTagName = "ITEM";
    private const string EntryTagName = "ENTRY";
    private const string KeyTagName = "KEY";
    private const string ValueTagName = "VALUE";

    private readonly XmlWriter _xmlWriter;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(XmlOutput));

    public XmlOutput(Stream stream)
    {
        _xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Async = true,
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
        await DisposeAsync();
        _logger.LogTrace("XML closed.");
    }

    /// <inheritdoc />
    protected override ValueTask<ErrorCode> OnWriteAsync(VariantValue[] values, CancellationToken cancellationToken = default)
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
            WriteXmlVariantValue(_xmlWriter, values[i]);
            _xmlWriter.WriteEndElement();
        }
        _xmlWriter.WriteEndElement(); // RowTagName.

        return ValueTask.FromResult(ErrorCode.OK);
    }

    private static void WriteXmlVariantValue(XmlWriter xmlWriter, in VariantValue value)
    {
        if (value.IsNull)
        {
            return;
        }

        switch (value.Type)
        {
            case DataType.Boolean:
                xmlWriter.WriteValue(value.AsBooleanUnsafe);
                break;
            case DataType.Float:
                xmlWriter.WriteValue(value.AsFloatUnsafe);
                break;
            case DataType.Integer:
                xmlWriter.WriteValue(value.AsIntegerUnsafe);
                break;
            case DataType.Numeric:
                xmlWriter.WriteValue(value.AsNumericUnsafe);
                break;
            case DataType.String:
                xmlWriter.WriteValue(value.AsStringUnsafe);
                break;
            case DataType.Timestamp:
                xmlWriter.WriteValue(value.AsTimestampUnsafe);
                break;
            case DataType.Interval:
                xmlWriter.WriteValue(value.AsIntervalUnsafe.ToString());
                break;
            case DataType.Object:
                WriteXmlObjectValue(xmlWriter, value);
                break;
            case DataType.Array:
                WriteXmlArrayValue(xmlWriter, value);
                break;
            case DataType.Map:
                WriteXmlMapValue(xmlWriter, value);
                break;
            default:
                xmlWriter.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                break;
        }
    }

    private static void WriteXmlObjectValue(XmlWriter xmlWriter, in VariantValue value)
    {
        var obj = value.AsObjectUnsafe;
        switch (obj)
        {
            case JsonElement jsonElement:
                WriteXmlJsonElement(xmlWriter, jsonElement);
                break;
            case JsonNode jsonNode:
                WriteXmlJsonNode(xmlWriter, jsonNode);
                break;
            default:
                xmlWriter.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                break;
        }
    }

    private static void WriteXmlJsonElement(XmlWriter xmlWriter, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    xmlWriter.WriteStartElement(SanitizeTagName(property.Name));
                    WriteXmlJsonElement(xmlWriter, property.Value);
                    xmlWriter.WriteEndElement();
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    xmlWriter.WriteStartElement(ItemTagName);
                    WriteXmlJsonElement(xmlWriter, item);
                    xmlWriter.WriteEndElement();
                }
                break;
            case JsonValueKind.String:
                xmlWriter.WriteValue(element.GetString());
                break;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                {
                    xmlWriter.WriteValue(longValue);
                }
                else if (element.TryGetDouble(out var doubleValue))
                {
                    xmlWriter.WriteValue(doubleValue);
                }
                else
                {
                    xmlWriter.WriteValue(element.GetRawText());
                }
                break;
            case JsonValueKind.True:
                xmlWriter.WriteValue(true);
                break;
            case JsonValueKind.False:
                xmlWriter.WriteValue(false);
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                break;
            default:
                xmlWriter.WriteValue(element.GetRawText());
                break;
        }
    }

    private static void WriteXmlJsonNode(XmlWriter xmlWriter, JsonNode? node)
    {
        if (node is null)
        {
            return;
        }

        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject)
                {
                    xmlWriter.WriteStartElement(SanitizeTagName(property.Key));
                    WriteXmlJsonNode(xmlWriter, property.Value);
                    xmlWriter.WriteEndElement();
                }
                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    xmlWriter.WriteStartElement(ItemTagName);
                    WriteXmlJsonNode(xmlWriter, item);
                    xmlWriter.WriteEndElement();
                }
                break;
            case JsonValue jsonValue:
                if (jsonValue.TryGetValue<int>(out var intValue))
                {
                    xmlWriter.WriteValue(intValue);
                }
                else if (jsonValue.TryGetValue<long>(out var longValue))
                {
                    xmlWriter.WriteValue(longValue);
                }
                else if (jsonValue.TryGetValue<double>(out var doubleValue))
                {
                    xmlWriter.WriteValue(doubleValue);
                }
                else if (jsonValue.TryGetValue<decimal>(out var decimalValue))
                {
                    xmlWriter.WriteValue(decimalValue);
                }
                else if (jsonValue.TryGetValue<bool>(out var boolValue))
                {
                    xmlWriter.WriteValue(boolValue);
                }
                else if (jsonValue.TryGetValue<DateTime>(out var dateTimeValue))
                {
                    xmlWriter.WriteValue(dateTimeValue);
                }
                else if (jsonValue.TryGetValue<string>(out var stringValue))
                {
                    xmlWriter.WriteValue(stringValue);
                }
                else
                {
                    xmlWriter.WriteValue(jsonValue.ToJsonString());
                }
                break;
            default:
                xmlWriter.WriteValue(node.ToJsonString());
                break;
        }
    }

    private static void WriteXmlArrayValue(XmlWriter xmlWriter, in VariantValue value)
    {
        var array = value.AsArrayUnsafe;
        for (var i = 0; i < array.Count; i++)
        {
            xmlWriter.WriteStartElement(ItemTagName);
            WriteXmlVariantValue(xmlWriter, array[i]);
            xmlWriter.WriteEndElement();
        }
    }

    private static void WriteXmlMapValue(XmlWriter xmlWriter, in VariantValue value)
    {
        var map = value.AsMapUnsafe;
        foreach (var kvp in map)
        {
            xmlWriter.WriteStartElement(EntryTagName);
            xmlWriter.WriteStartElement(KeyTagName);
            WriteXmlVariantValue(xmlWriter, kvp.Key);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteStartElement(ValueTagName);
            WriteXmlVariantValue(xmlWriter, kvp.Value);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// Sanitizes a string to be a valid XML element name.
    /// </summary>
    private static string SanitizeTagName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "_";
        }

        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 0)
            {
                if (!XmlConvert.IsStartNCNameChar(chars[i]))
                {
                    chars[i] = '_';
                }
            }
            else
            {
                if (!XmlConvert.IsNCNameChar(chars[i]))
                {
                    chars[i] = '_';
                }
            }
        }

        var result = new string(chars);
        if (result.Length >= 3
            && (result[0] == 'x' || result[0] == 'X')
            && (result[1] == 'm' || result[1] == 'M')
            && (result[2] == 'l' || result[2] == 'L'))
        {
            result = "_" + result;
        }

        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _xmlWriter.WriteEndElement(); // RootTagName.
        _xmlWriter.WriteEndDocument();
        _xmlWriter.Flush();
        _xmlWriter.Close();
        _xmlWriter.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _xmlWriter.WriteEndElementAsync(); // RootTagName.
        await _xmlWriter.WriteEndDocumentAsync();
        await _xmlWriter.FlushAsync();
        _xmlWriter.Close();
        await _xmlWriter.DisposeAsync();
    }
}
