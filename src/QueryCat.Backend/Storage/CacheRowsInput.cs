using System.Diagnostics;
using Serilog;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The input caches input data and after reset reads it from memory instead.
/// </summary>
[DebuggerDisplay("Count = {TotalCacheEntries}, CacheReads = {CacheReads}, TotalReads = {TotalReads}")]
public sealed class CacheRowsInput : IRowsInput
{
    private const int ChunkSize = 4096;

    [DebuggerDisplay("Key = {Key}, IsExpired = {IsExpired}")]
    private sealed class CacheEntry
    {
        public CacheKey Key { get; }

        public DateTime ExpireAt { get; }

        public ChunkList<VariantValue> Cache { get; } = new(ChunkSize);

        public bool IsExpired => DateTime.UtcNow >= ExpireAt;

        public bool IsCompleted { get; private set; }

        public int CacheLength { get; set; }

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

    private readonly List<CacheEntry> _cacheEntries = new();
    private readonly IRowsInput _rowsInput;
    private bool[] _cacheReadMap;
    private int _rowIndex = -1;
    private CacheEntry? _currentCacheEntry;

    private QueryContext _queryContext = new EmptyQueryContext();

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    internal int CacheReads { get; private set; }

    internal int TotalReads { get; private set; }

    internal int TotalCacheEntries => _cacheEntries.Count;

    public CacheRowsInput(IRowsInput rowsInput)
    {
        _rowsInput = rowsInput;
        _cacheReadMap = Array.Empty<bool>();
    }

    /// <inheritdoc />
    public void Open() => _rowsInput.Open();

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
        _queryContext = queryContext;
        _rowsInput.SetContext(queryContext);
    }

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
        TotalReads++;

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
        if (!hasData
            || (cacheEntry.Key.Limit > 0 && _rowIndex + 1 >= cacheEntry.Key.Limit))
        {
            cacheEntry.Complete();
            _cacheEntries.Add(cacheEntry);
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
            TotalReads++;
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
        var key = new CacheKey(_queryContext);
        _currentCacheEntry = new CacheEntry(key, TimeSpan.FromMinutes(1));
        return _currentCacheEntry;
    }

    private void RemoveExpiredKeys()
    {
        for (var i = 0; i < _cacheEntries.Count; i++)
        {
            if (_cacheEntries[i].IsExpired)
            {
                _cacheEntries.RemoveAt(i);
            }
        }
    }

    private CacheEntry CreateOrGetCacheEntry()
    {
        RemoveExpiredKeys();

        var key = new CacheKey(_queryContext);
        foreach (var cacheEntry in _cacheEntries)
        {
            if (cacheEntry.Key.Match(key))
            {
                Log.Logger.Debug("Reuse existing cache with key {Key}.", key);
                return cacheEntry;
            }
        }

        return new CacheEntry(key, TimeSpan.FromSeconds(30));
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowIndex = -1;
        var newCacheKey = new CacheKey(_queryContext);
        _rowsInput.Reset();
        if (_currentCacheEntry != null)
        {
            // If we reset but persist the same key - just go ahead using existing input.
            if (_currentCacheEntry.Key.Equals(newCacheKey))
            {
                Log.Logger.Debug("Reuse previous cache with key {Key}.", _currentCacheEntry.Key);
                return;
            }
        }
        _currentCacheEntry = CreateOrGetCacheEntry();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("Cache", _rowsInput);
    }

    /// <inheritdoc />
    public override string ToString()
        => $"cache: {_rowsInput}, total = {TotalCacheEntries}, reads = {CacheReads}, total_reads = {TotalReads}";
}
