using System.Diagnostics;
using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Inputs;

[DebuggerDisplay("Key = {Key}, IsExpired = {IsExpired}")]
internal sealed class CacheEntry
{
    private const int ChunkSize = 4096;

    public CacheKey Key { get; }

    public DateTime ExpireAt { get; }

    public ChunkList<VariantValue> Cache { get; } = new(ChunkSize);

    public bool IsExpired => DateTime.UtcNow >= ExpireAt;

    public bool IsCompleted { get; private set; }

    public int CacheLength { get; set; }

    public bool HasCacheEntries => Cache.Count > 0;

    public int RefCount { get; set; } = 0;

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
