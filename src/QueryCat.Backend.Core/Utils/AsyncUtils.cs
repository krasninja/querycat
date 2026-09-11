using System.Runtime.CompilerServices;

using CallbackWithState = (System.Threading.SendOrPostCallback, object?);

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Helpers for asynchronous operations.
/// </summary>
public static class AsyncUtils
{
    // For reference:
    // - https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Context/AsyncContext.cs.
    // - https://github.com/tejacques/AsyncBridge/blob/master/src/AsyncBridge/AsyncHelper.cs.
    // - https://github.com/ravendb/ravendb/blob/v7.2/src/Raven.Client/Util/AsyncHelpers.cs.

    /// <summary>
    /// Provides a context for asynchronous operations.
    /// </summary>
    private sealed class ExclusiveSynchronizationContext : SynchronizationContext
    {
#if DEBUG
        private readonly int _id = IdGenerator.GetNext();
#endif

        private bool _done;
        private bool _closed;
        private readonly Queue<CallbackWithState> _postbackItems = new();

        /// <inheritdoc />
        public override void Send(SendOrPostCallback d, object? state)
        {
            throw new NotSupportedException("Cannot Send to the message loop thread.");
        }

        /// <inheritdoc />
        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_postbackItems)
            {
                if (!_closed)
                {
                    _postbackItems.Enqueue((d, state));
                    Monitor.Pulse(_postbackItems);
                    return;
                }
            }

            // Context is dead - do not enqueue anymore. It might happen if fire-and-forget task
            // was executed within thread.
            QueueToThreadPool((d, state));
        }

        internal void EndMessageLoop()
        {
            Post(self => ((ExclusiveSynchronizationContext)self!).Complete(), this);
        }

        internal void BeginMessageLoop()
        {
            while (true)
            {
                CallbackWithState item;
                lock (_postbackItems)
                {
                    while (!_done && _postbackItems.Count == 0)
                    {
                        Monitor.Wait(_postbackItems);
                    }
                    if (_done)
                    {
                        return;
                    }
                    item = _postbackItems.Dequeue();
                }

                // Execute task out of the lock.
                item.Item1.Invoke(item.Item2);
            }
        }

        /// <inheritdoc />
        public override SynchronizationContext CreateCopy() => this;

        internal void Complete()
        {
            lock (_postbackItems)
            {
                _done = true;
                Monitor.Pulse(_postbackItems);
            }
        }

        internal void Close()
        {
            CallbackWithState[] orphaned;
            lock (_postbackItems)
            {
                _closed = true;
                _done = true;
                if (_postbackItems.Count == 0)
                {
                    return;
                }
                orphaned = _postbackItems.ToArray();
                _postbackItems.Clear();
            }

            // Execute orphan postbacks otherwise their async methods would never complete otherwise.
            foreach (var item in orphaned)
            {
                QueueToThreadPool(item);
            }
        }

        private static void QueueToThreadPool(CallbackWithState item)
            => ThreadPool.UnsafeQueueUserWorkItem(
                static s => s.Item1.Invoke(s.Item2),
                item,
                preferLocal: false);

#if DEBUG
        /// <inheritdoc />
        public override string ToString() => $"Id = {_id}";
#endif
    }

    /// <summary>
    /// Executes an async Task method which has a <typeparamref name="TTask" /> type return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    /// <param name="state">State to pass to delegate.</param>
    /// <typeparam name="TTask">Task generic type.</typeparam>
    /// <typeparam name="TArg">Argument type.</typeparam>
    /// <returns>Task value.</returns>
    private static TTask RunInternal<TArg, TTask>(Func<TArg, TTask> taskFunc, TArg state)
        where TTask : Task
    {
        var current = SynchronizationContext.Current;
        var exclusiveSynchronizationContext = new ExclusiveSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(exclusiveSynchronizationContext);

        try
        {
            var task = taskFunc.Invoke(state)
                   ?? throw new InvalidOperationException("The task delegate returned null.");
            if (!task.IsCompleted)
            {
                task.ConfigureAwait(false).GetAwaiter()
                    .UnsafeOnCompleted(exclusiveSynchronizationContext.Complete);
                exclusiveSynchronizationContext.BeginMessageLoop();
            }
            return task;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(current);
            exclusiveSynchronizationContext.Close();
        }
    }

    /// <summary>
    /// Executes an async Task method synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    public static void RunSync(Func<Task> taskFunc)
    {
        ArgumentNullException.ThrowIfNull(taskFunc);
        RunInternal(static s => s.Invoke(), taskFunc).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an async Task method which has a void return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static void RunSync(Func<CancellationToken, Task> taskFunc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFunc);
        RunInternal(static s => s.Func(s.Token), (Func: taskFunc, Token: cancellationToken))
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an async Task method which has a T return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    /// <typeparam name="TTask">Task generic type.</typeparam>
    /// <returns>Task value.</returns>
    public static TTask RunSync<TTask>(Func<Task<TTask>> taskFunc)
    {
        ArgumentNullException.ThrowIfNull(taskFunc);
        return RunInternal(static s => s.Invoke(), taskFunc).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an async Task method which has a T return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TTask">Task generic type.</typeparam>
    /// <returns>Task value.</returns>
    public static TTask RunSync<TTask>(Func<CancellationToken, Task<TTask>> taskFunc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFunc);
        return RunInternal(static s => s.Func.Invoke(s.State), (Func: taskFunc, State: cancellationToken))
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an async Task method which has a T return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    /// <param name="state">Argument.</param>
    /// <typeparam name="TArg">Argument type.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <returns>Task value.</returns>
    public static TResult RunSync<TArg, TResult>(Func<TArg, Task<TResult>> taskFunc, TArg state)
    {
        ArgumentNullException.ThrowIfNull(taskFunc);
        return RunInternal(static s => s.Func.Invoke(s.State), (Func: taskFunc, State: state))
            .GetAwaiter().GetResult();
    }

#if NET8_0
    /// <summary>
    /// Converts async enumerable into list.
    /// </summary>
    /// <param name="items">Async enumerable.</param>
    /// <param name="cancellationToken">Cancellation token to monitor request cancellation.</param>
    /// <typeparam name="T">Enumerable type.</typeparam>
    /// <returns>List.</returns>
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        var results = new List<T>();
        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            results.Add(item);
        }
        return results;
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if no element is found.
    /// </summary>
    /// <param name="items">Async enumerable to return an element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="T">Enumerable type.</typeparam>
    /// <returns>The first element or null.</returns>
    public static async Task<T?> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            return item;
        }
        return default;
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if no element is found.
    /// </summary>
    /// <param name="items">Async enumerable to return an element.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="T">Enumerable type.</typeparam>
    /// <returns>The first element or default.</returns>
    public static async Task<T> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> items,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            return item;
        }
        return defaultValue;
    }
#endif

    /// <summary>
    /// Convert <see cref="IEnumerable{T}" /> to <see cref="IAsyncEnumerable{T}" />.
    /// </summary>
    /// <param name="source">Source enumerable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <returns>Instance of <see cref="IAsyncEnumerable{TSource}" />.</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    // ReSharper disable once AsyncMethodWithoutAwait
    public static async IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(
        IEnumerable<TSource> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private sealed class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        /// <inheritdoc />
        public T Current => default!;

        /// <inheritdoc />
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(false);
    }

    private sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public static EmptyAsyncEnumerable<T> Instance { get; } = new();

        private static readonly EmptyAsyncEnumerator<T> _enumerator = new();

        /// <inheritdoc />
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => _enumerator;
    }

    /// <summary>
    /// Convert <see cref="IEnumerable{T}" /> to <see cref="IAsyncEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <returns>Instance of <see cref="IAsyncEnumerable{TSource}" />.</returns>
    public static IAsyncEnumerable<TSource> Empty<TSource>() => EmptyAsyncEnumerable<TSource>.Instance;
}
