using System.Reflection;

namespace QueryCat.Backend.Core.Utils;

using EventTask = System.Tuple<System.Threading.SendOrPostCallback, object?>;
using EventQueue = System.Collections.Concurrent.ConcurrentQueue<System.Tuple<System.Threading.SendOrPostCallback, object?>>;

/// <summary>
/// Helpers for asynchronous operations.
/// </summary>
public static class AsyncUtils
{
    // For reference:
    // - https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Context/AsyncContext.cs.
    // - https://github.com/tejacques/AsyncBridge/blob/master/src/AsyncBridge/AsyncHelper.cs.

    /// <summary>
    /// Provides a context for asynchronous operations.
    /// </summary>
    private sealed class ExclusiveSynchronizationContext : SynchronizationContext, IDisposable
    {
        private bool _done;

        private readonly AutoResetEvent _workItemsWaiting = new(initialState: false);

        private readonly EventQueue _postbackItems;

        public Exception? InnerException { get; internal set; }

        public ExclusiveSynchronizationContext(SynchronizationContext? oldContext)
        {
            if (oldContext is ExclusiveSynchronizationContext exclusiveSynchronizationContext)
            {
                this._postbackItems = exclusiveSynchronizationContext._postbackItems;
            }
            else
            {
                this._postbackItems = new EventQueue();
            }
        }

        /// <inheritdoc />
        public override void Send(SendOrPostCallback d, object? state)
        {
            throw new NotSupportedException("We cannot send to our same thread.");
        }

        /// <inheritdoc />
        public override void Post(SendOrPostCallback d, object? state)
        {
            _postbackItems.Enqueue(new EventTask(d, state));
            _workItemsWaiting.Set();
        }

        internal void EndMessageLoop()
        {
            Post(_ =>
            {
                Dispose();
            }, null);
        }

        internal void BeginMessageLoop()
        {
            while (!_done)
            {
                if (!_postbackItems.TryDequeue(out EventTask? task))
                {
                    task = null;
                }
                if (task != null)
                {
                    task.Item1(task.Item2);
                    if (InnerException != null)
                    {
                        throw new AggregateException("AsyncUtils.Run method threw an exception.", InnerException);
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
            _done = true;
            _postbackItems.Clear();
            _workItemsWaiting.Dispose();
        }
    }

    /// <summary>
    /// Executes an async Task method which has a void return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    public static void RunSync(Func<Task> taskFunc)
    {
        var current = SynchronizationContext.Current;
        var exclusiveSynchronizationContext = new ExclusiveSynchronizationContext(current);
        SynchronizationContext.SetSynchronizationContext(exclusiveSynchronizationContext);
        // ReSharper disable once AsyncVoidLambda
        exclusiveSynchronizationContext.Post(async _ =>
        {
            try
            {
                await taskFunc();
            }
            catch (AggregateException ex)
            {
                exclusiveSynchronizationContext.InnerException = ex.InnerException;
            }
            catch (TargetInvocationException ex)
            {
                exclusiveSynchronizationContext.InnerException = ex.InnerException;
            }
            catch (Exception ex)
            {
                exclusiveSynchronizationContext.InnerException = ex;
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
    /// <param name="taskFunc">Task.</param>
    public static void RunSync(Func<CancellationToken, Task> taskFunc)
        => RunSync(() => taskFunc.Invoke(CancellationToken.None));

    /// <summary>
    /// Executes an async Task method which has a T return value synchronously.
    /// </summary>
    /// <param name="taskFunc">Task.</param>
    /// <typeparam name="T">Task generic type.</typeparam>
    /// <returns>Task value.</returns>
    public static T? RunSync<T>(Func<Task<T>> taskFunc)
    {
        var current = SynchronizationContext.Current;
        var exclusiveSynchronizationContext = new ExclusiveSynchronizationContext(current);
        SynchronizationContext.SetSynchronizationContext(exclusiveSynchronizationContext);
        T? result = default(T);
        // ReSharper disable once AsyncVoidLambda
        exclusiveSynchronizationContext.Post(async _ =>
        {
            try
            {
                result = await taskFunc();
            }
            catch (AggregateException ex)
            {
                exclusiveSynchronizationContext.InnerException = ex.InnerException;
            }
            catch (TargetInvocationException ex)
            {
                exclusiveSynchronizationContext.InnerException = ex.InnerException;
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
    public static T? RunSync<T>(Func<ValueTask<T>> task)
    {
        return task.Invoke().GetAwaiter().GetResult();
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
