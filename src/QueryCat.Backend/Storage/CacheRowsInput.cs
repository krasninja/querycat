using System.Diagnostics;
using QueryCat.Backend.Logging;
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

        public void Finish()
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
        if (_rowIndex < cacheEntry.CacheLength && _cacheReadMap[columnIndex])
        {
            value = cacheEntry.Cache[offset];
            CacheReads++;
            return ErrorCode.OK;
        }

        var errorCode = _rowsInput.ReadValue(columnIndex, out value);
        cacheEntry.Cache[offset] = value;
        _cacheReadMap[columnIndex] = true;
        return errorCode;
    }

    private void IncreaseCache(CacheEntry cacheEntry)
    {
        for (int i = 0; i < Columns.Length; i++)
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

        if (!hasData
            || (cacheEntry.Key.Limit > 0 &&_rowIndex + 1 >= cacheEntry.Key.Limit))
        {
            cacheEntry.Finish();
            _cacheEntries.Add(cacheEntry);
        }

        return hasData;
    }

    private CacheEntry GetCurrentCacheEntry()
    {
        if (_currentCacheEntry != null)
        {
            return _currentCacheEntry;
        }
        var key = _queryContext.GetCacheKey();
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

        var key = _queryContext.GetCacheKey();
        foreach (var cacheEntry in _cacheEntries)
        {
            if (cacheEntry.Key.Match(key))
            {
                Logger.Instance.Debug("Reuse existing cache.", nameof(CacheRowsInput));
                return cacheEntry;
            }
        }

        return new CacheEntry(key, TimeSpan.FromSeconds(30));
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowIndex = -1;
        _rowsInput.Reset();
        var newCacheKey = _queryContext.GetCacheKey();
        if (_currentCacheEntry != null)
        {
            if (!_currentCacheEntry.IsCompleted && _currentCacheEntry.Key.Equals(newCacheKey))
            {
                Logger.Instance.Debug("Reuse previous cache.", nameof(CacheRowsInput));
                return;
            }
        }
        _currentCacheEntry = CreateOrGetCacheEntry();
    }
}
