using System.Collections.Concurrent;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Simple object pool implementation based on Microsoft.Extensions.ObjectPool.
/// </summary>
/// <typeparam name="T">Pool object type.</typeparam>
internal class SimpleObjectPool<T> where T : class
{
    // Based on .NET implementation: https://github.com/dotnet/dotnet/blob/main/src/aspnetcore/src/ObjectPool/src/DefaultObjectPool.cs.

    private readonly Func<T> _createFunc;
    private readonly Action<T>? _beforeReturn;
    private readonly int _maxCapacity;
    private int _numItems;

#pragma warning disable SA1401
    // ReSharper disable InconsistentNaming
    private protected readonly ConcurrentQueue<T> _items = new();
    private protected T? _fastItem;
    // ReSharper restore InconsistentNaming
#pragma warning restore SA1401

    /// <summary>
    /// Creates an instance of <see cref="SimpleObjectPool{T}" />.
    /// </summary>
    /// <param name="createFunc">Object factory function.</param>
    /// <param name="beforeReturn">The action is called before return object to the pool.</param>
    /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
    public SimpleObjectPool(Func<T> createFunc, Action<T>? beforeReturn = null, int maximumRetained = -1)
    {
        _createFunc = createFunc;
        _beforeReturn = beforeReturn;
        if (maximumRetained < 0)
        {
            maximumRetained = Environment.ProcessorCount * 2;
        }
        _maxCapacity = maximumRetained - 1;
    }

    /// <summary>
    /// Gets an object from the pool if one is available, otherwise creates one.
    /// </summary>
    /// <returns>A <typeparamref name="T" />.</returns>
    public virtual T Get()
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
    public virtual void Return(T obj) => ReturnCore(obj);

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <returns>True if the object was returned to the pool.</returns>
    private protected bool ReturnCore(T obj)
    {
        _beforeReturn?.Invoke(obj);
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
