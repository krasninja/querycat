using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Helpers for asynchronous operations.
/// </summary>
public static class AsyncUtils
{
    // For reference:
    // - https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Context/AsyncContext.cs.
    // - https://github.com/tejacques/AsyncBridge/blob/master/src/AsyncBridge/AsyncHelper.cs.
    // - https://github.com/ravendb/ravendb/blob/v7.1/src/Raven.Client/Util/AsyncHelpers.cs.

    private static readonly DisposableObjectPool<AutoResetEvent> _autoResetEventPool = new(
        createFunc: () => new AutoResetEvent(initialState: false),
        maximumRetained: 16,
        beforeReturn: c =>
        {
            c.Reset();
        }
    );

    /// <summary>
    /// Provides a context for asynchronous operations.
    /// </summary>
    private sealed class ExclusiveSynchronizationContext : SynchronizationContext, IDisposable
    {
#if DEBUG
        private readonly int _id = IdGenerator.GetNext();
#endif

        private sealed class CallbackWithState(SendOrPostCallback callback, object? state)
        {
            public SendOrPostCallback Callback { get; } = callback;

            public object? State { get; } = state;
        }

        private bool _done;
#pragma warning disable CA2213
        private readonly AutoResetEvent _workItemsWaiting;
#pragma warning restore CA2213
        private readonly ConcurrentQueue<CallbackWithState> _postbackItems = new();
        private volatile bool _isDisposed;

        public Exception? InnerException { get; internal set; }

        public Delegate Delegate { get; }

        public object? State { get; internal set; }

        public ExclusiveSynchronizationContext(Delegate @delegate, object? state = null)
        {
            Delegate = @delegate;
            State = state;

            _workItemsWaiting = _autoResetEventPool.Get();
        }

        /// <inheritdoc />
        public override void Send(SendOrPostCallback d, object? state)
        {
            throw new NotSupportedException("We cannot send to our same thread.");
        }

        /// <inheritdoc />
        public override void Post(SendOrPostCallback d, object? state)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            _postbackItems.Enqueue(new CallbackWithState(d, state));
            _workItemsWaiting.Set();
        }

        internal void EndMessageLoop()
        {
            Post(self =>
            {
                ((ExclusiveSynchronizationContext)self!)._done = true;
            }, this);
        }

        internal void BeginMessageLoop()
        {
            while (!_done)
            {
                if (_postbackItems.TryDequeue(out CallbackWithState? task))
                {
                    task.Callback.Invoke(task.State);
                    if (InnerException != null)
                    {
                        ThrowAggregateExceptionIfNeeded();
                    }
                }
                else
                {
                    _workItemsWaiting.WaitOne();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowAggregateExceptionIfNeeded()
        {
            if (InnerException != null)
            {
                throw new AggregateException("AsyncUtils.Run method threw an exception.", InnerException);
            }
        }

        /// <inheritdoc />
        public override SynchronizationContext CreateCopy() => new ForwardingSynchronizationContext(this);

        /// <summary>
        /// A lightweight copy that forwards <see cref="Post" /> to the original context while it is
        /// still alive, and falls back to the thread pool after the original has been disposed.
        /// This prevents <see cref="ObjectDisposedException" /> from captured copies and avoids
        /// posting work into a dead message loop.
        /// </summary>
        private sealed class ForwardingSynchronizationContext(ExclusiveSynchronizationContext owner) : SynchronizationContext
        {
            /// <inheritdoc />
            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotSupportedException("We cannot send to our same thread.");
            }

            /// <inheritdoc />
            public override void Post(SendOrPostCallback d, object? state)
            {
                if (!owner._isDisposed)
                {
                    try
                    {
                        owner.Post(d, state);
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Owner was disposed between our check and the call — fall through.
                    }
                }

                ThreadPool.QueueUserWorkItem(s => d(s), state);
            }

            /// <inheritdoc />
            public override SynchronizationContext CreateCopy() => this;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            _done = true;
            _postbackItems.Clear();
            _autoResetEventPool.Return(_workItemsWaiting);
        }

#if DEBUG
        /// <inheritdoc />
        public override string ToString() => $"Id = {_id}";
#endif
    }

    /// <summary>
    /// Executes an async Task method which has a void return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    public static void RunSync(Func<Task> taskFunc)
    {
        var current = SynchronizationContext.Current;
        var exclusiveSynchronizationContext = new ExclusiveSynchronizationContext(taskFunc);
        SynchronizationContext.SetSynchronizationContext(exclusiveSynchronizationContext);

        // ReSharper disable once AsyncVoidLambda
        exclusiveSynchronizationContext.Post(async context =>
        {
            var localContext = (ExclusiveSynchronizationContext)context!;

            try
            {
                await ((Func<Task>)localContext.Delegate!).Invoke();
            }
            catch (TargetInvocationException ex)
            {
                localContext.InnerException = ex.InnerException;
            }
            catch (Exception ex)
            {
                localContext.InnerException = ex is AggregateException ae && ae.InnerExceptions.Count > 0
                    ? ae.InnerExceptions[0]
                    : ex;
            }
            finally
            {
                localContext.EndMessageLoop();
            }
        }, exclusiveSynchronizationContext);

        try
        {
            exclusiveSynchronizationContext.BeginMessageLoop();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(current);
            exclusiveSynchronizationContext.Dispose();
        }
    }

    /// <summary>
    /// Executes an async Task method which has a void return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RunSync(Func<CancellationToken, Task> taskFunc)
        => RunSync(() => taskFunc.Invoke(CancellationToken.None));

    /// <summary>
    /// Executes an async Task method which has a <see cref="T" /> return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    /// <param name="state">State to pass to func.</param>
    /// <typeparam name="T">Task generic type.</typeparam>
    /// <returns>Task value.</returns>
    public static T? RunSync<T>(Func<object?, Task<T>> taskFunc, object? state)
    {
        var current = SynchronizationContext.Current;
        var exclusiveSynchronizationContext = new ExclusiveSynchronizationContext(taskFunc, state);
        SynchronizationContext.SetSynchronizationContext(exclusiveSynchronizationContext);

        // ReSharper disable once AsyncVoidLambda
        exclusiveSynchronizationContext.Post(async context =>
        {
            var localContext = (ExclusiveSynchronizationContext)context!;

            try
            {
                localContext.State = await ((Func<object?, Task<T>>)localContext.Delegate!)
                    .Invoke(localContext.State);
            }
            catch (TargetInvocationException ex)
            {
                localContext.InnerException = ex.InnerException;
            }
            catch (Exception ex)
            {
                localContext.InnerException = ex is AggregateException ae && ae.InnerExceptions.Count > 0
                    ? ae.InnerExceptions[0]
                    : ex;
            }
            finally
            {
                localContext.EndMessageLoop();
            }
        }, exclusiveSynchronizationContext);

        try
        {
            exclusiveSynchronizationContext.BeginMessageLoop();
            return (T?)exclusiveSynchronizationContext.State;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(current);
            exclusiveSynchronizationContext.Dispose();
        }
    }

    /// <summary>
    /// Executes an async Task method which has a T return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    /// <typeparam name="T">Task generic type.</typeparam>
    /// <returns>Task value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? RunSync<T>(Func<Task<T>> taskFunc) => RunSync(_ => taskFunc(), null);

    /// <summary>
    /// Executes an async Task method which has a T return value synchronously.
    /// </summary>
    /// <param name="task">Task.</param>
    /// <typeparam name="T">Task generic type.</typeparam>
    /// <returns>Task value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? RunSync<T>(Func<CancellationToken, Task<T>> task)
        => RunSync(() => task.Invoke(CancellationToken.None));

#if NET8_0 || NET9_0
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
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <returns>Instance of <see cref="IAsyncEnumerable{TSource}" />.</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public static async IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(IEnumerable<TSource> source)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }

    private sealed class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        /// <inheritdoc />
        public T Current => throw new InvalidOperationException();

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
