using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.ThriftPlugins;

/// <summary>
/// Queue with the ability of the async dequeue and return back to queue.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
internal sealed class WaitQueue : IDisposable
{
    private static readonly SimpleObjectPool<WaitingConsumer> _waitingConsumerPool = new(() => new WaitingConsumer());
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
    /// Constructor.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    public WaitQueue(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(WaitQueue));
    }

    /// <summary>
    /// Dequeue and wait for the available item.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Item session wrapper.</returns>
    public async ValueTask<ItemWrapper> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(WaitQueue));
        }

        // Has available client - use it.
        if (_availableItemsObjects.TryDequeue(out var clientWrapper))
        {
            return new ItemWrapper(this, clientWrapper);
        }

        // Create the new awaiter and wait.
        var awaiter = _waitingConsumerPool.Get();
        ItemWrapper itemWrapper;
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

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Take item {Item}, in use {InUseCount}, total {Count}.",
                itemWrapper.Item,
                InUseCount,
                Count
            );
        }

        // And then give it to the consumer.
        return itemWrapper;
    }

    /// <summary>
    /// Remove item from the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Item object.</returns>
    public async ValueTask<object> RemoveAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(WaitQueue));
        }

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
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(WaitQueue));
        }

        ReturnAvailableItemCore(item);
        Interlocked.Increment(ref _totalItemsCount);
    }

    private void ReturnAvailableItem(object item)
    {
        // Find the first
        ReturnAvailableItemCore(item);
        _logger.LogTrace("Return item {Item}.", item);
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

    private sealed class WaitingConsumer
    {
        public SemaphoreSlim Trigger { get; } = new(0, 1);

        public ItemWrapper? Wrapper { get; set; }
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
    }
}
