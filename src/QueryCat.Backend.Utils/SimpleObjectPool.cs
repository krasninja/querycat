using System.Collections.Concurrent;

namespace QueryCat.Backend.Utils;

/// <summary>
/// Simple object pool implementation based on Microsoft.Extensions.ObjectPool .
/// </summary>
/// <typeparam name="T">Pool object type.</typeparam>
internal sealed class SimpleObjectPool<T> where T : class
{
    // Based on .NET implementation: https://github.com/dotnet/aspnetcore/blob/main/src/ObjectPool/src/DefaultObjectPool.cs

    private readonly Func<T> _createFunc;
    private readonly int _maxCapacity;
    private int _numItems;

    private readonly ConcurrentQueue<T> _items = new();
    private T? _fastItem;

    /// <summary>
    /// Creates an instance of <see cref="SimpleObjectPool{T}" />.
    /// </summary>
    /// <param name="createFunc">Object factory function.</param>
    public SimpleObjectPool(Func<T> createFunc) : this(createFunc, Environment.ProcessorCount * 2)
    {
    }

    /// <summary>
    /// Creates an instance of <see cref="SimpleObjectPool{T}" />.
    /// </summary>
    /// <param name="createFunc">Object factory function.</param>
    /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
    public SimpleObjectPool(Func<T> createFunc, int maximumRetained)
    {
        _createFunc = createFunc;
        _maxCapacity = maximumRetained - 1;
    }

    /// <summary>
    /// Gets an object from the pool if one is available, otherwise creates one.
    /// </summary>
    /// <returns>A <typeparamref name="T" />.</returns>
    public T Get()
    {
        var item = _fastItem;
        if (item == null || Interlocked.CompareExchange(ref _fastItem, null, item) != item)
        {
            if (_items.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _numItems);
                return item;
            }

            // No object available, so go get a brand new one.
            return _createFunc();
        }

        return item;
    }

    /// <summary>
    /// Return an object to the pool.
    /// </summary>
    /// <param name="obj">The object to add to the pool.</param>
    public void Return(T obj) => ReturnCore(obj);

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <returns>True if the object was returned to the pool.</returns>
    private bool ReturnCore(T obj)
    {
        if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
        {
            if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
            {
                _items.Enqueue(obj);
                return true;
            }

            // No room, clean up the count and drop the object on the floor.
            Interlocked.Decrement(ref _numItems);
            return false;
        }

        return true;
    }
}
