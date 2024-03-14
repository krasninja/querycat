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
    public CacheEntry GetOrCreateEntry(CacheKey key)
    {
        RemoveExpiredAndIncompleteKeys();

        if (_entries.TryGetValue(key, out var existingKey))
        {
            _logger.LogDebug("Reuse existing cache entry with key {Key}.", key);
            return existingKey;
        }
        foreach (var cacheEntry in _entries.Values)
        {
            if (cacheEntry.Key.Match(key))
            {
                _logger.LogDebug("Reuse existing cache entry with key {Key}.", key);
                return cacheEntry;
            }
        }

        var entry = new CacheEntry(key, _expireTime);
        _entries.Add(key, entry);
        entry.RefCount++;
        _logger.LogDebug("Create new cache entry with key {Key}.", key);
        return entry;
    }

    private void RemoveExpiredAndIncompleteKeys()
    {
        var toRemove = _entries
            .Where(ce => ce.Value.RefCount == 0 && (ce.Value.IsExpired || !ce.Value.IsCompleted))
            .Select(ce => ce.Key)
            .ToList();
        for (var i = 0; i < toRemove.Count; i++)
        {
            _entries.Remove(toRemove[i]);
        }
    }
}
