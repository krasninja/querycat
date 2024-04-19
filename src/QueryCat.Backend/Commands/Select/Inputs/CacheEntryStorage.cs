using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal sealed class CacheEntryStorage : ICacheEntryStorage
{
    private static readonly TimeSpan _expireTime = TimeSpan.FromMinutes(1);

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(CacheEntryStorage));
    private readonly Dictionary<CacheKey, CacheEntry> _entries = new();

    /// <inheritdoc />
    public int Count => _entries.Count;

    /// <inheritdoc />
    public bool GetOrCreateEntry(CacheKey key, out CacheEntry entry)
    {
        RemoveExpiredAndIncompleteKeys();

        if (_entries.TryGetValue(key, out entry!))
        {
            _logger.LogDebug("Reuse existing cache entry with key {Key}.", key);
            return false;
        }
        foreach (var cacheEntry in _entries.Values)
        {
            if (cacheEntry.Key.Match(key))
            {
                _logger.LogDebug("Reuse existing cache entry with key {Key}.", key);
                return false;
            }
        }

        entry = new CacheEntry(key, _expireTime);
        _entries.Add(key, entry);
        _logger.LogDebug("Create new cache entry with key {Key}.", key);
        return true;
    }

    private void RemoveExpiredAndIncompleteKeys()
    {
        var toRemove = _entries
            .Where(ce => ce.Value.RefCount == 0 && (ce.Value.IsExpired || !ce.Value.IsCompleted))
            .Select(ce => ce.Key)
            .ToList();
        foreach (var removeItem in toRemove)
        {
            _entries.Remove(removeItem);
        }
    }
}
