namespace QueryCat.Backend.Utils;

/// <summary>
/// Helpers for asynchronous operations.
/// </summary>
public static class AsyncUtils
{
    // For reference: https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Context/AsyncContext.cs.

    /// <summary>
    /// Provides a context for asynchronous operations.
    /// </summary>
    private sealed class ExclusiveSynchronizationContext : SynchronizationContext, IDisposable
    {
        private bool _done;

        private readonly AutoResetEvent _workItemsWaiting = new(initialState: false);

        private readonly Queue<Tuple<SendOrPostCallback, object?>> _postbackItems = new();

        public Exception? InnerException { get; set; }

        /// <inheritdoc />
        public override void Send(SendOrPostCallback d, object? state)
        {
            throw new NotSupportedException("We cannot send to our same thread.");
        }

        /// <inheritdoc />
        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_postbackItems)
            {
                _postbackItems.Enqueue(Tuple.Create(d, state));
            }
            _workItemsWaiting.Set();
        }

        public void EndMessageLoop()
        {
            Post(_ =>
            {
                _done = true;
            }, null);
        }

        public void BeginMessageLoop()
        {
            while (!_done)
            {
                Tuple<SendOrPostCallback, object?>? postback = null;
                lock (_postbackItems)
                {
                    if (_postbackItems.Count > 0)
                    {
                        postback = _postbackItems.Dequeue();
                    }
                }
                if (postback != null)
                {
                    postback.Item1(postback.Item2);
                    if (InnerException != null)
                    {
                        throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                    }
                }
                else
                {
                    _workItemsWaiting.WaitOne();
                }
            }
        }

        /// <inheritdoc />
        public override SynchronizationContext CreateCopy() => this;

        /// <inheritdoc />
        public void Dispose()
        {
            _workItemsWaiting.Dispose();
        }
    }

    /// <summary>
    /// Executes an async Task method which has a void return value synchronously.
    /// </summary>
    /// <param name="task">Task.</param>
    public static void RunSync(Func<Task> task)
    {
        var current = SynchronizationContext.Current;
        var exclusiveSynchronizationContext = new ExclusiveSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(exclusiveSynchronizationContext);
        // ReSharper disable once AsyncVoidLambda
        exclusiveSynchronizationContext.Post(async _ =>
        {
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                var exception = ex;
                exclusiveSynchronizationContext.InnerException = exception;
                throw;
            }
            finally
            {
                exclusiveSynchronizationContext.EndMessageLoop();
            }
        }, null);
        exclusiveSynchronizationContext.BeginMessageLoop();
        SynchronizationContext.SetSynchronizationContext(current);
    }

    /// <summary>
    /// Executes an async Task method which has a void return value synchronously.
    /// </summary>
    /// <param name="task">Task.</param>
    public static void RunSync(Func<CancellationToken, Task> task)
        => RunSync(() => task.Invoke(CancellationToken.None));

    /// <summary>
    /// Executes an async Task method which has a void return value synchronously.
    /// </summary>
    /// <param name="task">Task.</param>
    public static void RunSyncValueTask(Func<ValueTask> task)
    {
        RunSync(() => task.Invoke().AsTask());
    }

    /// <summary>
    /// Executes an async Task method which has a T return value synchronously.
    /// </summary>
    /// <param name="task">Task.</param>
    /// <typeparam name="T">Task generic type.</typeparam>
    /// <returns>Task value.</returns>
    public static T? RunSync<T>(Func<Task<T>> task)
    {
        var current = SynchronizationContext.Current;
        var exclusiveSynchronizationContext = new ExclusiveSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(exclusiveSynchronizationContext);
        T? result = default(T);
        // ReSharper disable once AsyncVoidLambda
        exclusiveSynchronizationContext.Post(async _ =>
        {
            try
            {
                result = await task();
            }
            catch (Exception ex)
            {
                var exception = ex;
                exclusiveSynchronizationContext.InnerException = exception;
                throw;
            }
            finally
            {
                exclusiveSynchronizationContext.EndMessageLoop();
                exclusiveSynchronizationContext.Dispose();
            }
        }, null);
        exclusiveSynchronizationContext.BeginMessageLoop();
        SynchronizationContext.SetSynchronizationContext(current);
        return result;
    }

    /// <summary>
    /// Executes an async Task method which has a T return value synchronously.
    /// </summary>
    /// <param name="task">Task.</param>
    /// <typeparam name="T">Task generic type.</typeparam>
    /// <returns>Task value.</returns>
    public static T? RunSync<T>(Func<CancellationToken, Task<T>> task)
        => RunSync(() => task.Invoke(CancellationToken.None));

    /// <summary>
    /// Executes an async Task method which has a T return value synchronously.
    /// </summary>
    /// <param name="task">Task.</param>
    public static T? RunSyncValueTask<T>(Func<ValueTask<T>> task)
    {
        return RunSync(() => task.Invoke().AsTask());
    }

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
}