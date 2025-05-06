namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Simple object pool implementation based on Microsoft.Extensions.ObjectPool with dispose functionality.
/// </summary>
/// <typeparam name="T">Pool object type.</typeparam>
internal sealed class DisposableObjectPool<T> : SimpleObjectPool<T>, IDisposable where T : class
{
    // Based on .NET implementation: https://github.com/dotnet/aspnetcore/blob/main/src/ObjectPool/src/DisposableObjectPool.cs

    private volatile bool _isDisposed;

    /// <summary>
    /// Creates an instance of <see cref="DisposableObjectPool{T}" />.
    /// </summary>
    /// <param name="createFunc">Object factory function.</param>
    /// <param name="beforeReturn">The action is called before return object to the pool.</param>
    /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
    public DisposableObjectPool(Func<T> createFunc, Action<T>? beforeReturn = null, int maximumRetained = -1)
        : base(createFunc, beforeReturn, maximumRetained)
    {
    }

    /// <inheritdoc />
    public override T Get()
    {
        if (_isDisposed)
        {
            ThrowObjectDisposedException();
        }

        return base.Get();

        void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <inheritdoc />
    public override void Return(T obj)
    {
        // When the pool is disposed or the obj is not returned to the pool, dispose it
        if (_isDisposed || !ReturnCore(obj))
        {
            DisposeItem(obj);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _isDisposed = true;

        DisposeItem(_fastItem);
        _fastItem = null;

        while (_items.TryDequeue(out var item))
        {
            DisposeItem(item);
        }
    }

    private static void DisposeItem(T? item)
    {
        if (item is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
