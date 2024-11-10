using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace QueryCat.Backend.Utils;

/// <summary>
/// The version of dictionary that hold small amount of data. It uses array to search for keys.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
[DebuggerDisplay("Count = {Count}")]
internal sealed class SmallDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
{
    private TKey[] _keys;
    private TValue[] _values;
    private int _size;

    /// <inheritdoc />
    public int Count => _size;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public ICollection<TKey> Keys => _keys;

    /// <inheritdoc />
    public ICollection<TValue> Values => _values;

    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get
        {
            var i = IndexOfKey(key);
            if (i >= 0)
            {
                return _values[i];
            }
            throw new KeyNotFoundException("The given key was not present in the dictionary.");
        }

        set
        {
            ArgumentNullException.ThrowIfNull(key);
            var i = IndexOfKey(key);
            if (i >= 0)
            {
                _values[i] = value;
                return;
            }
            Add(key, value);
        }
    }

    public SmallDictionary() : this(0)
    {
    }

    public SmallDictionary(int capacity)
    {
        _keys = new TKey[capacity];
        _values = new TValue[capacity];
        _size = capacity;
    }

    private void Grow(int desiredSize)
    {
        var currentCapacity = _keys.Length;
        if (desiredSize <= currentCapacity)
        {
            return;
        }
        var newSize = (currentCapacity > 0 ? currentCapacity : 1) * 2;

        var newKeys = new TKey[newSize];
        var newValues = new TValue[newSize];
        if (currentCapacity > 0)
        {
            Array.Copy(_keys, newKeys, currentCapacity);
            Array.Copy(_values, newValues, currentCapacity);
        }
        _keys = newKeys;
        _values = newValues;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _size = 0;
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        var index = IndexOfKey(item.Key);
        if (index < 0)
        {
            return false;
        }
        var value = _values[index];
        if (value == null && item.Value == null)
        {
            return true;
        }
        if (value == null && item.Value != null)
        {
            return false;
        }
        return _values[index]!.Equals(item.Value);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        Array.Copy(_keys, 0, array, arrayIndex, _keys.Length);
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <inheritdoc />
    public void Add(TKey key, TValue value)
    {
        Grow(_size + 1);
        _keys[_size] = key;
        _values[_size] = value;
        _size++;
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => IndexOfKey(key) > -1;

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    /// <inheritdoc />
    public bool Remove(TKey key) => throw new NotSupportedException("Removing is not supported.");

    /// <inheritdoc />
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var index = IndexOfKey(key);
        if (index < 0)
        {
            value = default;
            return false;
        }
        value = _values[index];
        return true;
    }

    private int IndexOfKey(TKey key) => Array.IndexOf(_keys, key, 0, _size);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        for (var i = 0; i < _size; i++)
        {
            yield return new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
