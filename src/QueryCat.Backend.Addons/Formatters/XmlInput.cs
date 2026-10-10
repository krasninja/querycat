using System.Xml;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// XML input.
/// </summary>
internal sealed class XmlInput : IRowsInput, IDisposable
{
    private const int NoColumnIndex = -1;

    private XmlReader _xmlReader;
    private readonly StreamReader _streamReader;

    // State.
    private readonly Dictionary<int, List<VariantValue>> _cache = new();
    private int _cacheSize;
    private int _cacheRowIndex = -1; // Current cached row index, -1 if values are read from XML.
    private readonly List<VariantValue> _currentRow = new(); // Values for current row.
    private Column[] _columns = [];
    private bool _initMode; // In open mode we should fill cache.
    private bool _skipNextRead; // On next row read we do not need to read XML since it was done before.
    private readonly Stack<int> _attributesColumns = new(); // The stack is used to reset attribute values on tag close.

    private readonly string[] _uniqueKey;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(XmlInput));
    private bool _isDisposed;

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public string[] UniqueKey => _uniqueKey;

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    public XmlInput(Stream stream, string? xpath = null, params string[] uniqueKeys)
    {
        _streamReader = !string.IsNullOrEmpty(xpath)
            ? new StreamReader(RunXPath(stream, xpath))
            : new StreamReader(stream);
        _uniqueKey = uniqueKeys;
        if (!string.IsNullOrEmpty(xpath))
        {
            _uniqueKey = uniqueKeys.Concat([xpath]).ToArray();
        }

        _xmlReader = CreateXmlReader(_streamReader);
    }

    private static XmlReader CreateXmlReader(StreamReader streamReader)
    {
        return XmlReader.Create(streamReader, new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
            DtdProcessing = DtdProcessing.Ignore,
            ConformanceLevel = ConformanceLevel.Fragment,
            Async = true,
        });
    }

    private static Stream RunXPath(Stream stream, string xpath)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(reader);
        var nodes = xmlDocument.SelectNodes(xpath, GetXmlNamespaceManager(xmlDocument));
        if (nodes == null)
        {
            throw new QueryCatException("Cannot evaluate xpath.");
        }

        var memoryStream = new MemoryFileStream(stream);
        using var xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings
        {
            Async = false,
            Indent = false,
            NewLineHandling = NewLineHandling.None,
            ConformanceLevel = ConformanceLevel.Fragment,
        });

        if (nodes == null)
        {
            throw new QueryCatException("Cannot evaluate XPath query.");
        }
        foreach (XmlNode node in nodes)
        {
            node.WriteTo(xmlWriter);
        }
        xmlWriter.Flush();
        xmlWriter.Close();

        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }

    private static XmlNamespaceManager GetXmlNamespaceManager(XmlDocument xmlDocument)
    {
        // Adopted solution from here: https://www.codeproject.com/Messages/3665279/How-to-populate-an-XmlNamespaceManager.aspx
        var namespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
        var navigator = xmlDocument.CreateNavigator();
        if (navigator == null)
        {
            throw new InvalidOperationException("Cannot create XPath navigator.");
        }
        navigator.MoveToFollowing(XPathNodeType.Element);
        foreach (var ns in navigator.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml))
        {
            namespaceManager.AddNamespace(ns.Key, ns.Value);
        }
        return namespaceManager;
    }

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _initMode = true;

        // Read first rows.
        var count = 0;
        while (await ReadNextAsync(cancellationToken) && count++ < QueryContext.PrereadRowsCount)
        {
        }

        // Create frame and analyze types.
        var frame = new RowsFrame(Columns);
        var row = new Row(frame);
        for (var rowIndex = 0; rowIndex < _cacheSize; rowIndex++)
        {
            for (var colIndex = 0; colIndex < Columns.Length; colIndex++)
            {
                row[colIndex] = _cache[colIndex][rowIndex];
            }
            frame.AddRow(row);
        }
        await RowsIteratorUtils.ResolveColumnsTypesAsync(frame.GetIterator(), QueryContext.PrereadRowsCount,
            cancellationToken: cancellationToken);

        _initMode = false;
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _xmlReader.Close();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (_streamReader.BaseStream.CanSeek)
        {
            _streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
        }
        _streamReader.DiscardBufferedData();
        _xmlReader.Dispose();
        _xmlReader = CreateXmlReader(_streamReader);
        _attributesColumns.Clear();
        ClearCache();
        _cache.Clear();
        _cacheSize = 0;
        _cacheRowIndex = -1;
        _currentRow.Clear();
        _skipNextRead = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        VariantValue rawValue;
        if (_cacheRowIndex > -1)
        {
            var values = _cache[columnIndex];
            rawValue = _cacheRowIndex < values.Count ? values[_cacheRowIndex] : VariantValue.Null;
        }
        else
        {
            rawValue = columnIndex < _currentRow.Count ? _currentRow[columnIndex] : VariantValue.Null;
        }

        return ConvertValue(rawValue, Columns[columnIndex].DataType, out value);
    }

    private static ErrorCode ConvertValue(in VariantValue rawValue, DataType targetType, out VariantValue value)
    {
        if (rawValue.IsNull || targetType == DataType.String)
        {
            value = rawValue;
            return ErrorCode.OK;
        }
        if (!VariantValue.TryCreateFromString(rawValue.AsString, targetType, out value))
        {
            return ErrorCode.CannotCast;
        }
        return ErrorCode.OK;
    }

    /*
     * The read next problem is to determine where to stop reading and understand row limits. We process
     * two cases:
     * 1) Last tag repeat: ... <CITY>Krasnoyarsk</CITY><CITY>Delhi</CITY> ...
     * 2) Tag close repeat: ... <ROW><ID>412</ID><CITY>Moscow</CITY></ROW> ...
     */

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        var currentColumnName = string.Empty;
        var anyRead = false;

        if (!_initMode && _cacheSize > 0)
        {
            _cacheRowIndex++;
            if (_cacheRowIndex < _cacheSize)
            {
                return true;
            }
            // The cache is exhausted, continue reading XML.
            ClearCache();
        }

        ResetValuesOnElementEnd();

        try
        {
            while (_skipNextRead || await _xmlReader.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                _skipNextRead = false;

                // Enter to the tag, process attributes.
                if (_xmlReader.NodeType == XmlNodeType.Element)
                {
                    currentColumnName = _xmlReader.Name;
                    _attributesColumns.Push(NoColumnIndex);

                    if (_xmlReader.HasAttributes)
                    {
                        while (_xmlReader.MoveToNextAttribute())
                        {
                            var columnIndex = _initMode ? AddAndGetColumnIndex(_xmlReader.Name) : GetColumnIndex(_xmlReader.Name);
                            if (columnIndex < 0)
                            {
                                continue;
                            }
                            EnsureListSize(_currentRow, columnIndex + 1);
                            _currentRow[columnIndex] = new VariantValue(_xmlReader.Value);
                            _attributesColumns.Push(columnIndex);
                        }
                    }
                }
                // Read tag text value.
                else if (_xmlReader.NodeType == XmlNodeType.Text
                         && !string.IsNullOrEmpty(currentColumnName))
                {
                    var columnIndex = _initMode ? AddAndGetColumnIndex(currentColumnName) : GetColumnIndex(currentColumnName);
                    if (columnIndex < 0)
                    {
                        continue;
                    }
                    EnsureListSize(_currentRow, columnIndex + 1);
                    _currentRow[columnIndex] = new VariantValue(_xmlReader.Value.Trim());
                    anyRead = true;
                }
                else if (_xmlReader.NodeType == XmlNodeType.EndElement && anyRead)
                {
                    _skipNextRead = true; // No need to call Read() next time since it is done below.
                    while (await _xmlReader.ReadAsync()
                        && _xmlReader.NodeType != XmlNodeType.EndElement
                        && _xmlReader.NodeType != XmlNodeType.Element)
                    {
                    }

                    if (_xmlReader.EOF)
                    {
                        if (_initMode)
                        {
                            AddCacheRow();
                        }
                        return true;
                    }

                    if (_xmlReader.NodeType == XmlNodeType.EndElement
                        || (_xmlReader.NodeType == XmlNodeType.Element && _xmlReader.Name == currentColumnName))
                    {
                        if (_initMode)
                        {
                            AddCacheRow();
                        }
                        return true;
                    }
                }
            }
        }
        catch (XmlException e)
        {
            _logger.LogWarning("Cannot parse XML row: {Error}", e.Message);
            throw new QueryCatException(e.Message, e);
        }

        return false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine(nameof(XmlInput));
    }

    private static void EnsureListSize<T>(List<T?> list, int capacity)
    {
        while (list.Count < capacity)
        {
            list.Add(default);
        }
    }

    private int GetColumnIndex(string name)
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            if (_columns[i].Name == name)
            {
                return i;
            }
        }
        return -1;
    }

    private int AddAndGetColumnIndex(string name)
    {
        var index = GetColumnIndex(name);
        if (index == -1)
        {
            Array.Resize(ref _columns, _columns.Length + 1);
            _columns[^1] = new Column(name, DataType.String);
            _cache.Add(_columns.Length - 1, new List<VariantValue>());
            return _columns.Length - 1;
        }
        return index;
    }

    private void ResetValuesOnElementEnd()
    {
        while (_attributesColumns.TryPop(out var columnIndex)
               && columnIndex != NoColumnIndex)
        {
            _currentRow[columnIndex] = VariantValue.Null;
        }
    }

    private void AddCacheRow()
    {
        foreach (var cacheItem in _cache)
        {
            var missedCount = _cacheSize - cacheItem.Value.Count;
            for (var i = 0; i < missedCount; i++)
            {
                cacheItem.Value.Add(VariantValue.Null);
            }
        }

        _cacheSize++;
        for (var i = 0; i < _currentRow.Count; i++)
        {
            _cache[i].Add(_currentRow[i]);
        }
    }

    private void ClearCache()
    {
        // Keep the dictionary keys, they are mapped to the columns.
        foreach (var cacheItem in _cache.Values)
        {
            cacheItem.Clear();
        }
        _cacheSize = 0;
        _cacheRowIndex = -1;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        _xmlReader.Dispose();
        _streamReader.Dispose();
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => [];

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
    }
}
