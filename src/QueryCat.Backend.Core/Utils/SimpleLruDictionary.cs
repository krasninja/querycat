using System.Collections;
using System.Collections.Concurrent;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Simple LRU cache implementation using dictionary and linked list.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
internal sealed class SimpleLruDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull where TValue : class
{
    private readonly int _capacity;
    private readonly IDictionary<TKey, WeakReference<TValue>> _map;
    private readonly LinkedList<TKey> _lruList = [];

    /// <inheritdoc />
    public int Count => _map.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get
        {
            if (_map.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var target))
            {
                return target;
            }
            throw new KeyNotFoundException($"The key '{key}' was not found or the value has been garbage collected.");
        }

        set
        {
            if (!_map.TryAdd(key, new WeakReference<TValue>(value)))
            {
                _map[key] = new WeakReference<TValue>(value);
            }
            else
            {
                MakeKeyLast(key);
            }
            Evict();
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="capacity">Max number of items.</param>
    public SimpleLruDictionary(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));
        _capacity = capacity;
        _map = new ConcurrentDictionary<TKey, WeakReference<TValue>>();
    }

    /// <inheritdoc />
    public ICollection<TKey> Keys => _map.Keys;

    /// <inheritdoc />
    public ICollection<TValue> Values =>
        _map.Values
            .Select(wr => wr.TryGetTarget(out var t) ? t : null)
            .Where(v => v != null)
            .Select(v => v!)
            .ToList();

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <inheritdoc />
    public void Add(TKey key, TValue value)
    {
        _map.Add(key, new WeakReference<TValue>(value));
        _lruList.AddLast(key);
        Evict();
    }

    /// <inheritdoc />
    public void Clear()
    {
        _map.Clear();
        _lruList.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) => _map.ContainsKey(item.Key);

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => _map.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        foreach (var keyValue in _map)
        {
            if (keyValue.Value.TryGetTarget(out var target))
            {
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(keyValue.Key, target);
            }
        }
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        if (_map.Remove(key))
        {
            _lruList.Remove(key);
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_map.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var target))
        {
            value = target;
            return true;
        }
        value = null!;
        return false;
    }

    private void Evict()
    {
        while (_map.Count > _capacity)
        {
            var item = _lruList.First;
            if (item != null)
            {
                _map.Remove(item.Value);
                _lruList.RemoveFirst();
            }
        }
    }

    private void MakeKeyLast(TKey key)
    {
        _lruList.Remove(key);
        _lruList.AddLast(key);
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var keyValue in _map)
        {
            if (keyValue.Value.TryGetTarget(out var target))
            {
                yield return new KeyValuePair<TKey, TValue>(keyValue.Key, target);
            }
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
