using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QueryCat.Backend;

/// <summary>
/// A collection that stores list as a bunch of chunks.
/// </summary>
/// <typeparam name="T">The type of elements in the ChunkList collection.</typeparam>
[DebuggerDisplay("Count = {Count}")]
[Serializable]
internal sealed class ChunkList<T> : IList<T>, IList, IReadOnlyList<T>
{
    private sealed class Chunk<TChunk>
    {
        public int PrevCount { get; set; }

        public List<TChunk> Items { get; }

        public Chunk(int chunkSize)
        {
            Items = new List<TChunk>(chunkSize);
        }

        public int IndexOf(TChunk item) => PrevCount + Items.IndexOf(item);

        public void AddItem(TChunk item)
        {
            Items.Add(item);
        }

        public void RemoveItem(TChunk item)
        {
            Items.Remove(item);
        }
    }

    private readonly int _chunkSize;

    private int _count;

    private readonly List<Chunk<T>> _chunks = new();

    private int _lastItemIndex = -1;

    /// <inheritdoc cref="IList{T}.this" />
    public T this[int index]
    {
        get
        {
            var chunk = _chunks[FindChunk(index)];
            return chunk.Items[index - chunk.PrevCount];
        }

        set
        {
            var chunk = _chunks[FindChunk(index)];
            var itemIndex = index - chunk.PrevCount;
            chunk.Items[itemIndex] = value;
        }
    }

    /// <summary>
    /// Gets the number of items in the collection.
    /// </summary>
    /// <value>A number of items in the collection.</value>
    public int Count => _count;

    /// <inheritdoc />
    bool IList.IsReadOnly => false;

    /// <inheritdoc />
    bool ICollection<T>.IsReadOnly => false;

    /// <inheritdoc />
    object? IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value!;
    }

    /// <inheritdoc />
    bool IList.IsFixedSize => false;

    /// <inheritdoc />
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc />
    object ICollection.SyncRoot { get; } = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="chunkSize">The number of items in a chunk.</param>
    public ChunkList(int chunkSize)
    {
        this._chunkSize = chunkSize;
    }

    /// <inheritdoc />
    public int IndexOf(T item)
    {
        var count = 0;
        foreach (var chunk in _chunks)
        {
            var index = chunk.IndexOf(item);
            if (index > -1)
            {
                return index + count;
            }
            count++;
        }
        return -1;
    }

    private int IndexOf(Chunk<T> chunk, T item)
    {
        if (_lastItemIndex < 0 || _lastItemIndex >= _count || !this[_lastItemIndex]!.Equals(item))
        {
            _lastItemIndex = chunk.IndexOf(item);
        }
        return _lastItemIndex;
    }

    /// <inheritdoc />
    public void Insert(int index, T item)
    {
        if (index == 0 && _chunks.Count == 0)
        {
            Add(item);
            return;
        }

        var chunkIndexToInsert = FindChunk(index);
        var chunk = _chunks[chunkIndexToInsert];
        chunk.Items.Insert(index - chunk.PrevCount, item);
        _count++;
        if (chunk.Items.Count > _chunkSize * 1.5)
        {
            var newChunk = new Chunk<T>(_chunkSize);
            _chunks.Insert(_chunks.IndexOf(chunk) + 1, newChunk);
            var num2 = chunk.Items.Count / 2;
            while (num2 < chunk.Items.Count)
            {
                var item2 = chunk.Items[num2];
                chunk.RemoveItem(item2);
                newChunk.AddItem(item2);
            }
        }
        UpdatePrevCount(chunkIndexToInsert);
    }

    private void UpdatePrevCount(int startIndex)
    {
        var chunk = _chunks[startIndex];
        var count = chunk.PrevCount + chunk.Items.Count;
        for (var i = startIndex + 1; i < _chunks.Count; i++)
        {
            var chunk2 = _chunks[i];
            chunk2.PrevCount = count;
            count += chunk2.Items.Count;
        }
    }

    /// <summary>
    /// Removes an item at the specified position from the collection.
    /// </summary>
    /// <param name="index">A zero-based integer specifying the index of the object to remove.
    /// If it's negative or exceeds the number of elements, an exception is raised.</param>
    public void RemoveAt(int index)
    {
        var chunkIndex = FindChunk(index);
        var chunk = _chunks[chunkIndex];
        var indexAtChunk = index - chunk.PrevCount;
        chunk.Items.RemoveAt(indexAtChunk);
        _count--;
        if (chunk.Items.Count < _chunkSize)
        {
            if (chunkIndex > 0 && _chunks[chunkIndex - 1].Items.Count + chunk.Items.Count <= _chunkSize)
            {
                CombineChunks(chunkIndex - 1);
            }
            if (chunkIndex < _chunks.Count - 1 && _chunks[chunkIndex + 1].Items.Count + chunk.Items.Count <= _chunkSize)
            {
                CombineChunks(chunkIndex);
            }
        }
        UpdatePrevCount(chunkIndex > 0 ? chunkIndex - 1 : 0);
    }

    private void CombineChunks(int chunkIndex)
    {
        var chunk1 = _chunks[chunkIndex];
        var chunk2 = _chunks[chunkIndex + 1];
        while (chunk2.Items.Count > 0)
        {
            var item = chunk2.Items[0];
            chunk2.RemoveItem(item);
            chunk1.AddItem(item);
        }
        _chunks.RemoveAt(chunkIndex + 1);
    }

    private int FindChunk(int index)
    {
        var num = 0;
        var chunkIndex = _chunks.Count - 1;
        while (num < chunkIndex)
        {
            var num3 = num + (chunkIndex - num) / 2;
            if (index < _chunks[num3 + 1].PrevCount)
            {
                chunkIndex = num3;
            }
            else
            {
                num = num3 + 1;
            }
        }
        return chunkIndex;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if ((uint)_chunks.Count == 0 || (uint)_chunks[^1].Items.Count == (uint)_chunkSize)
        {
            _chunks.Add(new Chunk<T>(_chunkSize)
            {
                PrevCount = _count
            });
        }
        _chunks[^1].AddItem(item);
        _count++;
    }

    /// <summary>
    /// Clears the collection.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        foreach (var chunk in _chunks)
        {
            chunk.Items.Clear();
        }
        _chunks.Clear();
        _count = 0;
    }

    /// <inheritdoc />
    public bool Contains(T item) => IndexOf(item) != -1;

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        ((ICollection)this).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(T item)
    {
        RemoveAt(IndexOf(item));
        return true;
    }

    private IEnumerable<T> GetEnumerable()
    {
        foreach (var chunk in _chunks)
        {
            foreach (var item in chunk.Items)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Returns an <see cref="T:System.Collections.IDictionaryEnumerator" /> that can iterate through the hash table.
    /// </summary>
    /// <returns>An <see cref="T:System.Collections.IDictionaryEnumerator" /> for the hash table.</returns>
    public IEnumerator<T> GetEnumerator() => GetEnumerable().GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerable().GetEnumerator();

    /// <inheritdoc />
    int IList.Add(object? value)
    {
        Add((T)value!);
        return _count - 1;
    }

    /// <inheritdoc />
    bool IList.Contains(object? value) => Contains((T)value!);

    /// <inheritdoc />
    int IList.IndexOf(object? value) => IndexOf((T)value!);

    /// <inheritdoc />
    void IList.Insert(int index, object? value)
    {
        Insert(index, (T)value!);
    }

    /// <inheritdoc />
    void IList.Remove(object? value)
    {
        Remove((T)value!);
    }

    /// <inheritdoc />
    void ICollection.CopyTo(Array array, int index)
    {
        for (var i = 0; i < _count; i++)
        {
            array.SetValue(this[i], i + index);
        }
    }
}
