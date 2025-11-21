using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.ThriftPlugins;

/// <summary>
/// Queue with the ability of the async dequeue and return back to queue.
/// </summary>
[DebuggerDisplay("Count = {Count}, InUse = {InUseCount}, Available = {AvailableCount}")]
internal sealed partial class WaitQueue : IDisposable
{
    private static readonly DisposableObjectPool<WaitingConsumer> _waitingConsumerPool = new(
        createFunc: () => new WaitingConsumer(),
        beforeReturn: wc => wc.Check()
    );

    private readonly ConcurrentQueue<WaitingConsumer> _awaitClientQueue = new();
    private readonly ConcurrentQueue<object> _availableItemsObjects = new();
    private readonly ILogger _logger;
    private int _totalItemsCount;
    private bool _isDisposed;

    /// <summary>
    /// Total number of items in the queue (in use and available).
    /// </summary>
    public int Count => _totalItemsCount;

    /// <summary>
    /// Items in use.
    /// </summary>
    public int InUseCount => _totalItemsCount - _availableItemsObjects.Count;

    /// <summary>
    /// Number of awaiters for the available item.
    /// </summary>
    public int AwaitersCount => _awaitClientQueue.Count;

    /// <summary>
    /// Number of available for dequeue objects.
    /// </summary>
    public int AvailableCount => _availableItemsObjects.Count;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    public WaitQueue(ILoggerFactory? loggerFactory = null)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(nameof(WaitQueue));
    }

    /// <summary>
    /// Dequeue and wait for the available item.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Item session wrapper.</returns>
    public async ValueTask<ItemWrapper> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (TryDequeue(out var itemWrapper)
            && itemWrapper.HasValue)
        {
            return itemWrapper.Value;
        }

        // Create the new awaiter and wait.
        var awaiter = _waitingConsumerPool.Get();
        try
        {
            _awaitClientQueue.Enqueue(awaiter);
            await awaiter.Trigger.WaitAsync(cancellationToken);
            if (awaiter.Wrapper == null)
            {
                throw new InvalidOperationException("Wrapper is not set.");
            }
            itemWrapper = awaiter.Wrapper.Value;
        }
        finally
        {
            awaiter.Wrapper = null;
            _waitingConsumerPool.Return(awaiter);
        }

        // And then give it to the consumer.
        LogTakeItem(itemWrapper.Value, InUseCount, Count);
        return itemWrapper.Value;
    }

    /// <summary>
    /// Try to dequeue the item.
    /// </summary>
    /// <param name="itemWrapper">Item or null.</param>
    /// <returns><c>True</c> if was able to return, <c>false</c> otherwise.</returns>
    public bool TryDequeue(out ItemWrapper? itemWrapper)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        // Has available client - use it.
        if (_availableItemsObjects.TryDequeue(out var clientWrapper))
        {
            itemWrapper = new ItemWrapper(this, clientWrapper);
            LogTakeItem(itemWrapper.Value.Item, InUseCount, Count);
            return true;
        }

        itemWrapper = null;
        return false;
    }

    /// <summary>
    /// Remove item from the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Item object.</returns>
    public async ValueTask<object> RemoveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var itemWrapper = await DequeueAsync(cancellationToken);
        Interlocked.Decrement(ref _totalItemsCount);
        return itemWrapper.Item;
    }

    /// <summary>
    /// Increase number of available slots.
    /// </summary>
    /// <param name="item">Optional item to enqueue.</param>
    public void Enqueue(object item)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        ReturnAvailableItemCore(item);
        Interlocked.Increment(ref _totalItemsCount);
    }

    private void ReturnAvailableItem(object item)
    {
        ReturnAvailableItemCore(item);
        LogReturnItem(item);
    }

    private void ReturnAvailableItemCore(object item)
    {
        // Try to find the first awaitable client.
        if (_awaitClientQueue.TryDequeue(out var consumer))
        {
            consumer.Wrapper = new ItemWrapper(this, item);
            consumer.Trigger.Release();
        }
        else
        {
            _availableItemsObjects.Enqueue(item);
        }
    }

    [LoggerMessage(LogLevel.Trace, "Take item {Item}, in use {InUseCount}, total {Count}.")]
    private partial void LogTakeItem(object item, int inUseCount, int count);

    [LoggerMessage(LogLevel.Trace, "Return item {Item}.")]
    private partial void LogReturnItem(object item);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        foreach (var clientWrapper in _availableItemsObjects)
        {
            (clientWrapper as IDisposable)?.Dispose();
        }
        foreach (var waitingConsumer in _awaitClientQueue)
        {
            waitingConsumer.Trigger.Dispose();
        }
        _isDisposed = true;
    }

    private sealed class WaitingConsumer : IDisposable
    {
        public SemaphoreSlim Trigger { get; } = new(0, 1);

        public ItemWrapper? Wrapper { get; set; }

        internal void Check()
        {
            if (Trigger.CurrentCount != 0)
            {
                throw new InvalidOperationException("Trigger has not been initialized properly.");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Trigger.Dispose();
        }
    }

    /// <summary>
    /// Object item wrapper with the ability to return it into the queue with dispose.
    /// </summary>
    /// <param name="waitQueue">Instance of <see cref="WaitQueue" />.</param>
    /// <param name="item">Custom object item.</param>
    public readonly struct ItemWrapper(WaitQueue waitQueue, object item) : IDisposable
    {
        /// <summary>
        /// Queue item.
        /// </summary>
        public object Item => item;

        /// <inheritdoc />
        public void Dispose()
        {
            waitQueue.ReturnAvailableItem(item);
        }

        /// <inheritdoc />
        public override string ToString() => "Item = " + item;
    }
}
