using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// The version of dictionary that hold small amount of data. It uses array to search for keys.
/// </summary>
/// <remarks>
/// This class is not thread-safe. External synchronization is required for concurrent access.
/// </remarks>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
[DebuggerDisplay("Count = {Count}")]
internal sealed class SmallDictionary<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private TKey[] _keys;
    private TValue[] _values;
    private int _size;

    /// <inheritdoc cref="ICollection{T}.Count" />
    public int Count => _size;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public ICollection<TKey> Keys => new KeyCollection(this);

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc />
    public ICollection<TValue> Values => new ValueCollection(this);

    /// <inheritdoc cref="IDictionary{TKey,TValue}.this" />
    public TValue this[TKey key]
    {
        get
        {
            var i = IndexOfKey(key);
            if (i >= 0)
            {
                return _values[i];
            }
            throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");
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

    /// <summary>
    /// Initializes a new empty instance of <see cref="SmallDictionary{TKey, TValue}" />.
    /// </summary>
    public SmallDictionary()
    {
        _keys = [];
        _values = [];
        _size = 0;
    }

    /// <summary>
    /// Initializes a new instance with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">Initial number of elements the dictionary can hold without resizing.</param>
    public SmallDictionary(int capacity)
    {
        _keys = new TKey[capacity];
        _values = new TValue[capacity];
        _size = 0;
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
        Array.Clear(_keys, 0, _size);
        Array.Clear(_values, 0, _size);
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
        return EqualityComparer<TValue>.Default.Equals(_values[index], item.Value);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        for (var i = 0; i < _size; i++)
        {
            array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
        }
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <inheritdoc />
    public void Add(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (IndexOfKey(key) >= 0)
        {
            throw new ArgumentException($"An item with the same key '{key}' has already been added.");
        }
        Grow(_size + 1);
        _keys[_size] = key;
        _values[_size] = value;
        _size++;
    }

    /// <inheritdoc cref="IDictionary{TKey,TValue}.ContainsKey" />
    public bool ContainsKey(TKey key) => IndexOfKey(key) > -1;

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        var index = IndexOfKey(item.Key);
        if (index < 0)
        {
            return false;
        }
        if (!EqualityComparer<TValue>.Default.Equals(_values[index], item.Value))
        {
            return false;
        }
        throw new NotSupportedException("Removing is not supported.");
    }

    /// <inheritdoc />
    public bool Remove(TKey key) => throw new NotSupportedException("Removing is not supported.");

    /// <inheritdoc cref="IDictionary{TKey,TValue}.TryGetValue" />
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

    /// <summary>
    /// Get dictionary enumerator.
    /// </summary>
    /// <returns>Instance of <see cref="Enumerator" />.</returns>
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerates the elements of a <see cref="SmallDictionary{TKey, TValue}" />.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly SmallDictionary<TKey, TValue> _dictionary;
        private int _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(SmallDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_index < _dictionary._size)
            {
                _current = new KeyValuePair<TKey, TValue>(
                    _dictionary._keys[_index],
                    _dictionary._values[_index]);
                _index++;
                return true;
            }
            _current = default;
            return false;
        }

        /// <inheritdoc />
        public KeyValuePair<TKey, TValue> Current => _current;

        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Reset()
        {
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Represents the collection of keys in a <see cref="SmallDictionary{TKey, TValue}" />.
    /// </summary>
    private sealed class KeyCollection(SmallDictionary<TKey, TValue> dictionary) : ICollection<TKey>
    {
        /// <inheritdoc />
        public int Count => dictionary._size;

        /// <inheritdoc />
        public bool IsReadOnly => true;

        /// <inheritdoc />
        public void Add(TKey item) => throw new NotSupportedException();

        /// <inheritdoc />
        public void Clear() => throw new NotSupportedException();

        /// <inheritdoc />
        public bool Contains(TKey item) => dictionary.IndexOfKey(item) >= 0;

        /// <inheritdoc />
        public void CopyTo(TKey[] array, int arrayIndex)
        {
            Array.Copy(dictionary._keys, 0, array, arrayIndex, dictionary._size);
        }

        /// <inheritdoc />
        public bool Remove(TKey item) => throw new NotSupportedException();

        /// <inheritdoc />
        public IEnumerator<TKey> GetEnumerator()
        {
            for (var i = 0; i < dictionary._size; i++)
            {
                yield return dictionary._keys[i];
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Represents the collection of values in a <see cref="SmallDictionary{TKey, TValue}" />.
    /// </summary>
    private sealed class ValueCollection(SmallDictionary<TKey, TValue> dictionary) : ICollection<TValue>
    {
        /// <inheritdoc />
        public int Count => dictionary._size;

        /// <inheritdoc />
        public bool IsReadOnly => true;

        /// <inheritdoc />
        public void Add(TValue item) => throw new NotSupportedException();

        /// <inheritdoc />
        public void Clear() => throw new NotSupportedException();

        /// <inheritdoc />
        public bool Contains(TValue item)
        {
            for (var i = 0; i < dictionary._size; i++)
            {
                if (EqualityComparer<TValue>.Default.Equals(dictionary._values[i], item))
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            Array.Copy(dictionary._values, 0, array, arrayIndex, dictionary._size);
        }

        /// <inheritdoc />
        public bool Remove(TValue item) => throw new NotSupportedException();

        /// <inheritdoc />
        public IEnumerator<TValue> GetEnumerator()
        {
            for (var i = 0; i < dictionary._size; i++)
            {
                yield return dictionary._values[i];
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
