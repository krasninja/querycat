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
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_isOpened)
        {
            return;
        }
        await _rowsInput.OpenAsync(cancellationToken);
        _isOpened = true;
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (!_isOpened)
        {
            return;
        }
        await _rowsInput.CloseAsync(cancellationToken);
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

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        _rowIndex++;
        _cacheReadMap = _cacheReadMap.Length > 0 ? _cacheReadMap : new bool[_rowsInput.Columns.Length];
        var cacheEntry = await GetOrCreateCacheEntryAsync(cancellationToken);
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

        var hasData = await _rowsInput.ReadNextAsync(cancellationToken);
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
            await ReadAllItemsToCacheAsync(cancellationToken);
        }

        // If we don't have data - this means we can complete the cache line.
        if (!hasData)
        {
            cacheEntry.Complete();
        }

        return hasData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValue ReadValueByPosition(int rowIndex, int columnIndex)
    {
        if (_currentCacheEntry == null)
        {
            return VariantValue.Null;
        }
        var offset = (Columns.Length * rowIndex) + columnIndex;
        CacheReads++;
        return _currentCacheEntry.Cache[offset];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask<VariantValue> ReadValueByPositionAsync(int rowIndex, int columnIndex, CancellationToken cancellationToken)
    {
        var cacheEntry = await GetOrCreateCacheEntryAsync(cancellationToken);
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

    private async ValueTask ReadAllItemsToCacheAsync(CancellationToken cancellationToken)
    {
        var cacheEntry = await GetOrCreateCacheEntryAsync(cancellationToken);
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

    private async ValueTask<CacheEntry> GetOrCreateCacheEntryAsync(CancellationToken cancellationToken)
    {
        if (_resetRequested || _currentCacheEntry == null)
        {
            _resetRequested = false;
            var newCacheKey = await CreateCacheKeyAsync(_thread, _innerRowsInputType, _rowsInput,
                _queryContext, _conditions, cancellationToken);
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

    private static async ValueTask<CacheKey> CreateCacheKeyAsync(
        IExecutionThread thread,
        string from,
        IRowsInput rowsInput,
        QueryContext queryContext,
        SelectQueryConditions conditions,
        CancellationToken cancellationToken)
    {
        CacheKeyCondition[]? cacheConditions = null;
        if (rowsInput is IRowsInputKeys rowsInputKeys)
        {
            var keyConditions = conditions.GetKeyConditions(rowsInputKeys).ToArray();
            cacheConditions = new CacheKeyCondition[keyConditions.Length];
            for (var i = 0; i < keyConditions.Length; i++)
            {
                cacheConditions[i] = await keyConditions[i].ToCacheCondition(thread, cancellationToken);
            }
        }

        return new CacheKey(
            from: from,
            inputArguments: rowsInput.UniqueKey,
            selectColumns: queryContext.QueryInfo.Columns.Select(c => c.Name).ToArray(),
            conditions: cacheConditions,
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
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _rowIndex = -1;
        _resetRequested = true;
        return _rowsInput.ResetAsync(cancellationToken);
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

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        if (_rowsInput is IRowsInputKeys rowsInputKeys)
        {
            rowsInputKeys.UnsetKeyColumnValue(columnIndex, operation);
        }
    }
}
