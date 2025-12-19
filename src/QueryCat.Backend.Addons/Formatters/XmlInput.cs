using System.Xml;
using System.Xml.XPath;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// XML input.
/// </summary>
internal sealed class XmlInput : IRowsInput, IDisposable, IAsyncDisposable
{
    private const int NoColumnIndex = -1;

    private readonly XmlReader _xmlReader;

    // State.
    private readonly Dictionary<int, List<string>> _cache = new();
    private int _cacheSize;
    private readonly SortedList<int, VariantValue> _currentRow = new(); // Values for current row.
    // ReSharper disable once UseArrayEmptyMethod
    private Column[] _columns = [];
    private bool _initMode; // In open mode we should fill cache.
    private bool _skipNextRead; // On next row read we do not need to read XML since it was done before.
    private readonly Stack<int> _attributesColumns = new(); // The stack is used to reset attribute values on tag close.
    private readonly string[] _uniqueKey;

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public string[] UniqueKey => _uniqueKey;

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    public XmlInput(Stream stream, string? xpath = null, params string[] uniqueKeys)
    {
        var streamReader = !string.IsNullOrEmpty(xpath)
            ? new StreamReader(RunXPath(stream, xpath))
            : new StreamReader(stream);
        _uniqueKey = uniqueKeys;
        if (!string.IsNullOrEmpty(xpath))
        {
            _uniqueKey = uniqueKeys.Concat([xpath]).ToArray();
        }

        _xmlReader = XmlReader.Create(streamReader, new XmlReaderSettings
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
        using var streamReader = new StreamReader(stream);
        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(streamReader.ReadToEnd());
        var xmlNamespaceManager = GetXmlNamespaceManager(xmlDocument);
        var nodes = xmlDocument.SelectNodes(xpath, xmlNamespaceManager);

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
                row[colIndex] = new VariantValue(_cache[colIndex][rowIndex]);
            }
            frame.AddRow(row);
        }
        await RowsIteratorUtils.ResolveColumnsTypesAsync(frame.GetIterator(), QueryContext.PrereadRowsCount,
            cancellationToken: cancellationToken);

        _initMode = false;
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsync();
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (_xmlReader is not XmlTextReader xmlTextReader)
        {
            throw new InvalidOperationException("Reset is not supported.");
        }
        xmlTextReader.ResetState();
        _attributesColumns.Clear();
        _cache.Clear();
        _cacheSize = 0;
        _currentRow.Clear();
        _skipNextRead = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_cacheSize > 0)
        {
            var values = _cache[columnIndex];
            if (!VariantValue.TryCreateFromString(values[^(_cacheSize + 1)], Columns[columnIndex].DataType, out value))
            {
                return ErrorCode.CannotCast;
            }
        }
        else
        {
            if (!_currentRow.TryGetValue(columnIndex, out value))
            {
                value = VariantValue.Null;
            }
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
            _cacheSize--;
            return true;
        }

        ResetValuesOnElementEnd();

        while (_skipNextRead || await _xmlReader.ReadAsync())
        {
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
                        var colIndex = _initMode ? AddAndGetColumnIndex(_xmlReader.Name) : GetColumnIndex(_xmlReader.Name);
                        _currentRow[colIndex] = new VariantValue(_xmlReader.Value);
                        _attributesColumns.Push(colIndex);
                    }
                }
            }
            // Read tag text value.
            else if (_xmlReader.NodeType == XmlNodeType.Text
                     && !string.IsNullOrEmpty(currentColumnName))
            {
                var columnIndex = _initMode ? AddAndGetColumnIndex(currentColumnName) : GetColumnIndex(currentColumnName);
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

        return false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine(nameof(XmlInput));
    }

    private int GetColumnIndex(string name)
        => Array.FindIndex(_columns, c => c.Name.Equals(name));

    private int AddAndGetColumnIndex(string name)
    {
        var index = GetColumnIndex(name);
        if (index == -1)
        {
            Array.Resize(ref _columns, _columns.Length + 1);
            _columns[^1] = new Column(name, DataType.String);
            _cache.Add(_columns.Length - 1, new List<string>());
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
        foreach (var keyValuePair in _currentRow)
        {
            _cache[keyValuePair.Key].Add(keyValuePair.Value);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _xmlReader.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _xmlReader.Dispose();
        return ValueTask.CompletedTask;
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
