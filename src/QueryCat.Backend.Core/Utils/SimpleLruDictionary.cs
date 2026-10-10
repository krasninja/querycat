using System.Collections;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Simple thread-safe LRU cache implementation using dictionary and linked list.
/// Read and write operations mark the item as most recently used. When the capacity
/// is exceeded the least recently used item is evicted.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
internal sealed class SimpleLruDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _map;

    // The first item is the least recently used, the last is the most recently used.
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _lruList = [];
#if NET9_0_OR_GREATER
    private readonly Lock _objLock = new();
#else
    private readonly object _objLock = new();
#endif

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_objLock)
            {
                return _map.Count;
            }
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <summary>
    /// Max number of items.
    /// </summary>
    public int Capacity => _capacity;

    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }
            throw new KeyNotFoundException($"The key '{key}' was not found.");
        }

        set => AddOrUpdate(key, value, overwrite: true);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="capacity">Max number of items.</param>
    /// <param name="comparer">Optional keys' comparer.</param>
    public SimpleLruDictionary(int capacity, IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(comparer);
    }

    /// <inheritdoc />
    public ICollection<TKey> Keys
    {
        get
        {
            lock (_objLock)
            {
                return _lruList.Select(kv => kv.Key).ToList();
            }
        }
    }

    /// <inheritdoc />
    public ICollection<TValue> Values
    {
        get
        {
            lock (_objLock)
            {
                return _lruList.Select(kv => kv.Value).ToList();
            }
        }
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <inheritdoc />
    public void Add(TKey key, TValue value) => AddOrUpdate(key, value, overwrite: false);

    /// <inheritdoc />
    public void Clear()
    {
        lock (_objLock)
        {
            _map.Clear();
            _lruList.Clear();
        }
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        lock (_objLock)
        {
            return _map.TryGetValue(item.Key, out var node)
                && EqualityComparer<TValue>.Default.Equals(node.Value.Value, item.Value);
        }
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key)
    {
        lock (_objLock)
        {
            return _map.ContainsKey(key);
        }
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        lock (_objLock)
        {
            if (array.Length - arrayIndex < _lruList.Count)
            {
                throw new ArgumentException("Destination array is not long enough.", nameof(array));
            }
            _lruList.CopyTo(array, arrayIndex);
        }
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        lock (_objLock)
        {
            if (_map.TryGetValue(item.Key, out var node)
                && EqualityComparer<TValue>.Default.Equals(node.Value.Value, item.Value))
            {
                _map.Remove(item.Key);
                _lruList.Remove(node);
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        lock (_objLock)
        {
            if (_map.Remove(key, out var node))
            {
                _lruList.Remove(node);
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_objLock)
        {
            if (_map.TryGetValue(key, out var node))
            {
                MoveToLast(node);
                value = node.Value.Value;
                return true;
            }
        }
        value = default!;
        return false;
    }

    private void AddOrUpdate(TKey key, TValue value, bool overwrite)
    {
        lock (_objLock)
        {
            if (_map.TryGetValue(key, out var node))
            {
                if (!overwrite)
                {
                    throw new ArgumentException($"An item with the same key '{key}' has already been added.", nameof(key));
                }
                node.Value = new KeyValuePair<TKey, TValue>(key, value);
                MoveToLast(node);
                return;
            }

            if (_map.Count >= _capacity)
            {
                EvictFirst();
            }
            _map.Add(key, _lruList.AddLast(new KeyValuePair<TKey, TValue>(key, value)));
        }
    }

    private void EvictFirst()
    {
        var first = _lruList.First;
        if (first != null)
        {
            _map.Remove(first.Value.Key);
            _lruList.RemoveFirst();
        }
    }

    private void MoveToLast(LinkedListNode<KeyValuePair<TKey, TValue>> node)
    {
        if (node != _lruList.Last)
        {
            _lruList.Remove(node);
            _lruList.AddLast(node);
        }
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        // Enumerate the snapshot to avoid holding the lock and concurrent modification issues.
        KeyValuePair<TKey, TValue>[] items;
        lock (_objLock)
        {
            items = _lruList.ToArray();
        }
        return ((IEnumerable<KeyValuePair<TKey, TValue>>)items).GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
