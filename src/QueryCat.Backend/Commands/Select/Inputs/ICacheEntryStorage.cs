using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Inputs;

/// <summary>
/// The interface represents logic to store cache entries and keys dictionary.
/// </summary>
internal interface ICacheEntryStorage
{
    /// <summary>
    /// Total entries.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Get entry by key or create a new one.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <returns>Cache entry.</returns>
    CacheEntry GetOrCreateEntry(CacheKey key);
}
