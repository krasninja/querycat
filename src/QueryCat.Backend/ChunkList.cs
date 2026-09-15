using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace QueryCat.Backend;

/// <summary>
/// Append-only list that stores items in fixed-size array chunks. Unlike <see cref="List{T}" />
/// it never copies items on growth and keeps each chunk below the LOH threshold.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
[DebuggerDisplay("Count = {Count}")]
internal sealed class ChunkList<T> : IReadOnlyList<T>
{
    private readonly int _chunkShift;
    private readonly int _chunkMask;
    private readonly List<T[]> _chunks = new();
    private int _count;

    /// <inheritdoc />
    public int Count => _count;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="chunkSize">The number of items in a chunk, must be a power of two.</param>
    public ChunkList(int chunkSize)
    {
        if (chunkSize <= 0 || !BitOperations.IsPow2(chunkSize))
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be a positive power of two.");
        }
        _chunkShift = BitOperations.Log2((uint)chunkSize);
        _chunkMask = chunkSize - 1;
    }

    /// <inheritdoc cref="IReadOnlyList{T}.this" />
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
            {
                ThrowIndexOutOfRange();
            }
            return _chunks[index >> _chunkShift][index & _chunkMask];
        }

        set
        {
            if ((uint)index >= (uint)_count)
            {
                ThrowIndexOutOfRange();
            }
            _chunks[index >> _chunkShift][index & _chunkMask] = value;
        }
    }

    /// <summary>
    /// Add item.
    /// </summary>
    /// <param name="item">Item to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if ((_count & _chunkMask) == 0)
        {
            _chunks.Add(new T[_chunkMask + 1]);
        }
        _chunks[_count >> _chunkShift][_count & _chunkMask] = item;
        _count++;
    }

    /// <summary>
    /// Clear all data.
    /// </summary>
    public void Clear()
    {
        _chunks.Clear();
        _count = 0;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        var remaining = _count;
        foreach (var chunk in _chunks)
        {
            var length = Math.Min(chunk.Length, remaining);
            for (var i = 0; i < length; i++)
            {
                yield return chunk[i];
            }
            remaining -= length;
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void ThrowIndexOutOfRange() => throw new ArgumentOutOfRangeException("index");
}
