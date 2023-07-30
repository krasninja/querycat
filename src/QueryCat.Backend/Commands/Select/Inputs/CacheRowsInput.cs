using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Inputs;

/// <summary>
/// The input caches input data and after reset reads it from memory instead.
/// </summary>
[DebuggerDisplay("Count = {TotalCacheEntries}, CacheReads = {CacheReads}, TotalReads = {TotalReads}")]
internal sealed class CacheRowsInput : IRowsInputKeys
{
    private const int ChunkSize = 4096;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<CacheRowsInput>();

    [DebuggerDisplay("Key = {Key}, IsExpired = {IsExpired}")]
    private sealed class CacheEntry
    {
        public CacheKey Key { get; }

        public DateTime ExpireAt { get; }

        public ChunkList<VariantValue> Cache { get; } = new(ChunkSize);

        public bool IsExpired => DateTime.UtcNow >= ExpireAt;

        public bool IsCompleted { get; private set; }

        public int CacheLength { get; set; }

        public bool HasCacheEntries => Cache.Count > 0;

        public CacheEntry(CacheKey key, TimeSpan expireAt)
        {
            Key = key;
            ExpireAt = DateTime.UtcNow + expireAt;
        }

        public void Complete()
        {
            IsCompleted = true;
        }
    }

    private readonly Dictionary<CacheKey, CacheEntry> _cacheEntries = new();
    private readonly IRowsInput _rowsInput;
    private readonly SelectQueryConditions _conditions;
    private bool[] _cacheReadMap;
    private int _rowIndex = -1;
    private CacheEntry? _currentCacheEntry;
    private bool _hadReadNextCalls;
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
    public void Open() => _rowsInput.Open();

    /// <inheritdoc />
    public void Close() => _rowsInput.Close();

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_rowIndex < 0)
        {
            value = VariantValue.Null;
            return ErrorCode.Error;
        }

        var cacheEntry = GetCurrentCacheEntry();

        var offset = Columns.Length * _rowIndex + columnIndex;
        value = cacheEntry.Cache[offset];
        CacheReads++;
        return ErrorCode.OK;
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

        // If we don't have data - this mean we can complete the cache line.
        if (!hasData)
        {
            cacheEntry.Complete();
            _cacheEntries.Add(cacheEntry.Key, cacheEntry);
        }

        return hasData;
    }

    private void ReadAllItemsToCache()
    {
        var cacheEntry = GetCurrentCacheEntry();
        for (var columnIndex = 0; columnIndex < _cacheReadMap.Length; columnIndex++)
        {
            var offset = Columns.Length * _rowIndex + columnIndex;
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
            return _currentCacheEntry;
        }
        var key = new CacheKey(_rowsInput, _queryContext, _conditions);
        _currentCacheEntry = new CacheEntry(key, TimeSpan.FromMinutes(1));
        return _currentCacheEntry;
    }

    private void RemoveExpiredKeys()
    {
        var toRemove = _cacheEntries
            .Where(ce => ce.Value.IsExpired && ce.Value != _currentCacheEntry)
            .Select(ce => ce.Key)
            .ToList();
        for (var i = 0; i < toRemove.Count; i++)
        {
            _cacheEntries.Remove(toRemove[i]);
        }
    }

    private CacheEntry CreateOrGetCacheEntry()
    {
        RemoveExpiredKeys();

        var key = new CacheKey(_rowsInput, _queryContext, _conditions);
        // Fast path.
        if (_cacheEntries.TryGetValue(key, out var existingKey))
        {
            _logger.LogDebug("Reuse existing cache entry with key {Key}.", key);
            return existingKey;
        }
        foreach (var cacheEntry in _cacheEntries.Values)
        {
            if (cacheEntry.Key.Match(key))
            {
                _logger.LogDebug("Reuse existing cache entry with key {Key}.", key);
                return cacheEntry;
            }
        }

        var entry = new CacheEntry(key, TimeSpan.FromSeconds(120));
        _logger.LogDebug("Create new cache entry with key {Key}.", key);
        return entry;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowIndex = -1;
        var newCacheKey = new CacheKey(_rowsInput, _queryContext, _conditions);
        if (_currentCacheEntry != null)
        {
            // If we reset but persist the same key - just go ahead using existing input.
            if (_currentCacheEntry.Key.Equals(newCacheKey))
            {
                _logger.LogDebug("Reuse previous cache with key {Key}.", _currentCacheEntry.Key);
                return;
            }
        }
        _currentCacheEntry = CreateOrGetCacheEntry();
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
        => $"cache: {_rowsInput}, total = {TotalCacheEntries}, reads = {CacheReads}, total_reads = {TotalReads}";
}
