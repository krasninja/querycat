using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Inputs;

/// <summary>
/// The input caches input data and after reset reads it from memory instead.
/// </summary>
[DebuggerDisplay("Count = {TotalCacheEntries}, CacheReads = {CacheReads}, TotalReads = {TotalReads}")]
internal sealed class CacheRowsInput : IRowsInputKeys
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(CacheRowsInput));

#if DEBUG
    private readonly Guid _id = Guid.NewGuid();
#endif
    private readonly ICacheEntryStorage _cacheEntries = new CacheEntryStorage();
    private readonly IExecutionThread _thread;
    private readonly IRowsInput _rowsInput;
    private readonly SelectQueryConditions _conditions;
    private bool[] _cacheReadMap;
    private int _rowIndex = -1;
    private CacheEntry? _currentCacheEntry;
    private bool _hadReadNextCalls;
    private bool _resetRequested;
    private bool _isOpened;
    private QueryContext _queryContext;
    private readonly string _innerRowsInputType;

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

    public CacheRowsInput(IExecutionThread thread, IRowsInput rowsInput, SelectQueryConditions? conditions = null)
    {
        _thread = thread;
        _rowsInput = rowsInput;
        _innerRowsInputType = GetRowsInputId(_rowsInput);
        _conditions = conditions ?? new SelectQueryConditions();
        _cacheReadMap = [];
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
            return ErrorCode.NoData;
        }

        value = ReadValueByPosition(_rowIndex, columnIndex);
        return ErrorCode.OK;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValue ReadValueByPosition(int rowIndex, int columnIndex)
    {
        var cacheEntry = GetOrCreateCacheEntry();
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
        _rowIndex++;
        _cacheReadMap = _cacheReadMap.Length > 0 ? _cacheReadMap : new bool[_rowsInput.Columns.Length];
        var cacheEntry = GetOrCreateCacheEntry();
        _hadReadNextCalls = true;

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
        var cacheEntry = GetOrCreateCacheEntry();
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

    private CacheEntry GetOrCreateCacheEntry()
    {
        if (_resetRequested || _currentCacheEntry == null)
        {
            _resetRequested = false;
            var newCacheKey = CreateCacheKey(_thread, _innerRowsInputType, _rowsInput, _queryContext, _conditions);
            if (_currentCacheEntry != null)
            {
                // If we reset but persist the same key - just go ahead using existing input.
                if (!_currentCacheEntry.Key.Equals(newCacheKey))
                {
                    _currentCacheEntry.RefCount--;
                    _currentCacheEntry = null;
                }
                else
                {
                    _logger.LogDebug("Reuse previous cache with key {Key}.", _currentCacheEntry.Key);
                    return _currentCacheEntry;
                }
            }
            var isNew = _cacheEntries.GetOrCreateEntry(newCacheKey, out _currentCacheEntry);
            if (isNew && _hadReadNextCalls)
            {
                _hadReadNextCalls = false;
            }
            _currentCacheEntry.RefCount++;
        }

#if DEBUG
        var keyForCheck = CreateCacheKey(_thread, _innerRowsInputType, _rowsInput, _queryContext, _conditions);
        Debug.Assert(keyForCheck == _currentCacheEntry.Key, "Cache key has been changed!");
#endif
        return _currentCacheEntry;
    }

    private static CacheKey CreateCacheKey(
        IExecutionThread thread,
        string from,
        IRowsInput rowsInput,
        QueryContext queryContext,
        SelectQueryConditions conditions)
    {
        return new CacheKey(
            from: from,
            inputArguments: rowsInput.UniqueKey,
            selectColumns: queryContext.QueryInfo.Columns.Select(c => c.Name).ToArray(),
            conditions: rowsInput is IRowsInputKeys rowsInputKeys
                ? conditions.GetKeyConditions(rowsInputKeys).Select(c => c.ToCacheCondition(thread)).ToArray()
                : null,
            offset: queryContext.QueryInfo.Offset,
            limit: queryContext.QueryInfo.Limit);
    }

    private static string GetRowsInputId(IRowsInput rowsInput)
    {
        if (rowsInput is SetKeysRowsInput setKeysRowsInput)
        {
            return setKeysRowsInput.InnerRowsInput.GetType().Name;
        }
        return rowsInput.GetType().Name;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowIndex = -1;
        _resetRequested = true;
        _rowsInput.Reset();
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
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        if (_rowsInput is IRowsInputKeys rowsInputKeys)
        {
            rowsInputKeys.SetKeyColumnValue(columnIndex, value, operation);
        }
    }
}
