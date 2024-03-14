using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Inputs;

/// <summary>
/// The input caches input data and after reset reads it from memory instead.
/// </summary>
[DebuggerDisplay("Count = {TotalCacheEntries}, CacheReads = {CacheReads}, TotalReads = {TotalReads}")]
internal sealed class CacheRowsInput : IRowsInputKeys
{
    private static readonly TimeSpan _defaultLifeTime = TimeSpan.FromMinutes(1);

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(CacheRowsInput));

    private readonly ICacheEntryStorage _cacheEntries = new CacheEntryStorage();
    private readonly IRowsInput _rowsInput;
    private readonly SelectQueryConditions _conditions;
    private bool[] _cacheReadMap;
    private int _rowIndex = -1;
    private CacheEntry? _currentCacheEntry;
    private bool _hadReadNextCalls;
    private bool _isOpened;
    private QueryContext _queryContext;

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => _rowsInput.UniqueKey;

    internal int CacheReads { get; private set; }

    internal int InputReads { get; private set; }

    internal int TotalReads => CacheReads + InputReads;

    internal int TotalCacheEntries => _cacheEntries.Count;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _queryContext;
        set
        {
            _queryContext = value;
            _rowsInput.QueryContext = _queryContext;
        }
    }

    public CacheRowsInput(IRowsInput rowsInput, SelectQueryConditions? conditions = null)
    {
        _rowsInput = rowsInput;
        _conditions = conditions ?? new SelectQueryConditions();
        _cacheReadMap = Array.Empty<bool>();
        _queryContext = rowsInput.QueryContext;
    }

    /// <inheritdoc />
    public void Open()
    {
        if (_isOpened)
        {
            return;
        }
        _rowsInput.Open();
        _isOpened = true;
    }

    /// <inheritdoc />
    public void Close()
    {
        if (!_isOpened)
        {
            return;
        }
        _rowsInput.Close();
        _isOpened = false;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_rowIndex < 0)
        {
            value = VariantValue.Null;
            return ErrorCode.Error;
        }

        value = ReadValueByPosition(_rowIndex, columnIndex);
        return ErrorCode.OK;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValue ReadValueByPosition(int rowIndex, int columnIndex)
    {
        var cacheEntry = GetCurrentCacheEntry();
        var offset = (Columns.Length * rowIndex) + columnIndex;
        CacheReads++;
        return cacheEntry.Cache[offset];
    }

    private void IncreaseCache(CacheEntry cacheEntry)
    {
        for (var i = 0; i < Columns.Length; i++)
        {
            cacheEntry.Cache.Add(VariantValue.Null);
        }
        cacheEntry.CacheLength++;
        Array.Fill(_cacheReadMap, false);
    }

    /// <inheritdoc />
    public bool ReadNext()
    {
        _hadReadNextCalls = true;
        _rowIndex++;
        _cacheReadMap = _cacheReadMap.Length > 0 ? _cacheReadMap : new bool[_rowsInput.Columns.Length];
        var cacheEntry = GetCurrentCacheEntry();

        // Read cache data if possible.
        if (_rowIndex < cacheEntry.CacheLength)
        {
            Array.Fill(_cacheReadMap, true);
            return true;
        }
        // We cannot read cache data, but cache is completed.
        if (cacheEntry.IsCompleted)
        {
            return false;
        }

        var hasData = _rowsInput.ReadNext();
        if (hasData)
        {
            // Otherwise increase cache and read data.
            IncreaseCache(cacheEntry);
        }
        else
        {
            _rowIndex--;
        }

        if (hasData && _rowIndex > -1)
        {
            ReadAllItemsToCache();
        }

        // If we don't have data - this means we can complete the cache line.
        if (!hasData)
        {
            cacheEntry.Complete();
        }

        return hasData;
    }

    private void ReadAllItemsToCache()
    {
        var cacheEntry = GetCurrentCacheEntry();
        var baseOffset = Columns.Length * _rowIndex;
        for (var columnIndex = 0; columnIndex < _cacheReadMap.Length; columnIndex++)
        {
            var offset = baseOffset + columnIndex;
            _rowsInput.ReadValue(columnIndex, out var value);
            InputReads++;
            cacheEntry.Cache[offset] = value;
        }
        Array.Fill(_cacheReadMap, true);
    }

    private CacheEntry GetCurrentCacheEntry()
    {
        if (_currentCacheEntry != null)
        {
            Debug.Assert(CreateCacheKey(_rowsInput, _queryContext, _conditions) == _currentCacheEntry.Key, "Cache key has been changed!");
            return _currentCacheEntry;
        }
        var key = CreateCacheKey(_rowsInput, _queryContext, _conditions);
        _currentCacheEntry = new CacheEntry(key, _defaultLifeTime);
        return _currentCacheEntry;
    }

    private CacheKey CreateCacheKey() => CreateCacheKey(_rowsInput, _queryContext, _conditions);

    private static CacheKey CreateCacheKey(IRowsInput rowsInput, QueryContext queryContext, SelectQueryConditions conditions)
    {
        return new CacheKey(
            from: rowsInput.GetType().Name,
            inputArguments: rowsInput.UniqueKey,
            selectColumns: queryContext.QueryInfo.Columns.Select(c => c.Name).ToArray(),
            conditions: rowsInput is IRowsInputKeys rowsInputKeys
                ? conditions.GetKeyConditions(rowsInputKeys).Select(c => c.ToCacheCondition()).ToArray()
                : null,
            offset: queryContext.QueryInfo.Offset,
            limit: queryContext.QueryInfo.Limit);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowIndex = -1;
        var newCacheKey = CreateCacheKey();
        if (_currentCacheEntry != null)
        {
            // If we reset but persist the same key - just go ahead using existing input.
            if (_currentCacheEntry.Key.Equals(newCacheKey))
            {
                _logger.LogDebug("Reuse previous cache with key {Key}.", _currentCacheEntry.Key);
                return;
            }
            _currentCacheEntry.RefCount--;
        }
        _currentCacheEntry = _cacheEntries.GetOrCreateEntry(newCacheKey);
        if (_hadReadNextCalls)
        {
            _hadReadNextCalls = false;
            _rowsInput.Reset();
        }
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("Cache", _rowsInput);
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns()
    {
        if (_rowsInput is IRowsInputKeys rowsInputKeys)
        {
            return rowsInputKeys.GetKeyColumns();
        }
        return new List<KeyColumn>();
    }

    /// <inheritdoc />
    public void SetKeyColumnValue(string columnName, VariantValue value, VariantValue.Operation operation)
    {
        if (_rowsInput is IRowsInputKeys rowsInputKeys)
        {
            rowsInputKeys.SetKeyColumnValue(columnName, value, operation);
        }
    }

    /// <inheritdoc />
    public override string ToString()
        => $"Cache = {_rowsInput}, Count = {TotalCacheEntries}, Reads = {CacheReads}, TotalReads = {TotalReads}";
}
