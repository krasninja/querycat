using System.Diagnostics;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Inputs;

[DebuggerDisplay("Key = {Key}")]
internal sealed class CacheEntry
{
    private const int ChunkSize = 4096;

    public CacheKey Key { get; }

    public ChunkList<VariantValue> Cache { get; } = new(ChunkSize);

    public bool IsCompleted { get; private set; }

    public int CacheLength { get; set; }

    public bool HasCacheEntries => Cache.Count > 0;

    public int RefCount { get; set; }

    public CacheEntry(CacheKey key)
    {
        Key = key;
    }

    public void Complete()
    {
        IsCompleted = true;
    }
}
