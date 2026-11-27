using Xunit;
using QueryCat.Backend.Utils;

namespace QueryCat.IntegrationTests.Execution;

/// <summary>
/// Tests for <see cref="AsyncLock" />.
/// </summary>
public class AsyncLockTests
{
    [Fact]
    public async Task Lock_RecursiveCall_ShouldSupportReentrancy()
    {
        // Arrange.
        var asyncLock = new AsyncLock();
        var resource = 10;

        // Act.
        await using (await asyncLock.LockAsync(TestContext.Current.CancellationToken))
        {
            await using (await asyncLock.LockAsync(TestContext.Current.CancellationToken))
            {
                await Task.Delay(20, TestContext.Current.CancellationToken);
                await using (await asyncLock.LockAsync(TestContext.Current.CancellationToken))
                {
                    resource = 20;
                }
                await using (await asyncLock.LockAsync(TestContext.Current.CancellationToken))
                {
                    resource = 30;
                }
            }
        }

        // Assert.
        Assert.Equal(30, resource);
    }

    [Fact]
    public async Task Lock_RecursiveCallSyncAndAsync_ShouldSupportReentrancy()
    {
        // Arrange.
        var asyncLock = new AsyncLock();
        var resource = 10;

        // Act.
        await using (await asyncLock.LockAsync(TestContext.Current.CancellationToken))
        {
            using (asyncLock.Lock(TestContext.Current.CancellationToken))
            {
                await Task.Delay(20, TestContext.Current.CancellationToken);
                await using (await asyncLock.LockAsync(TestContext.Current.CancellationToken))
                {
                    resource = 20;
                }
                using (asyncLock.Lock(TestContext.Current.CancellationToken))
                {
                    resource = 30;
                }
            }
        }

        // Assert.
        Assert.Equal(30, resource);
    }

    [Fact]
    public async Task Lock_CanLock()
    {
        // Arrange.
        var asyncLock = new AsyncLock();
        var resource = 10;

        // Act.
        var task1 = Task.Run(async () =>
        {
            await using (await asyncLock.LockAsync())
            {
                Thread.Sleep(1000);
                resource = 20;
            }
        }, TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var task2 = Task.Run(async () =>
        {
            await using (await asyncLock.LockAsync())
            {
                resource = 30;
            }
        }, TestContext.Current.CancellationToken);
        await task1;
        await task2;

        // Assert.
        Assert.Equal(30, resource);
    }

    [Fact]
    public async Task Lock_CanMultipleLock()
    {
        // Arrange.
        var asyncLock = new AsyncLock();
        var resource = 0;
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            var task = Task.Run(async () =>
            {
                for (var j = 0; j < 10; j++)
                {
                    await using (await asyncLock.LockAsync())
                    {
                        resource++;
                    }
                }
            }, TestContext.Current.CancellationToken);
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        // Assert.
        Assert.Equal(1000, resource);
    }

    [Fact]
    public async Task Lock_CanMultipleLockWithYield()
    {
        // Arrange.
        var asyncLock = new AsyncLock();
        var guard = false;
        var tasks = new List<Task>();

        for (var i = 0; i < 200; i++)
        {
            var task = Task.Run(async () =>
            {
                await using (await asyncLock.LockAsync())
                {
                    Assert.False(guard);
                    guard = true;
                    SynchronizationContext.SetSynchronizationContext(null);
                    await Task.Yield(); // Return to the task pool.
                    guard = false;
                }
            }, TestContext.Current.CancellationToken);
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Lock_SupportsMultipleAsynchronousLocks()
    {
        // Act.
        await Task.Run(() =>
        {
            var asyncLock = new AsyncLock();
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var task1 = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await using (await asyncLock.LockAsync())
                    {
                        Thread.Sleep(10);
                    }
                }
            });
            var task2 = Task.Run(() =>
            {
                using (asyncLock.Lock())
                {
                    Thread.Sleep(1000);
                }
            });

            task2.Wait();
            cancellationTokenSource.Cancel();
            task1.Wait();
        }, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Lock_CancellationSupport()
    {
        // Arrange.
        var asyncLock = new AsyncLock();
        var cts = new CancellationTokenSource();
        var endLoopIndicator = true;

        var task1 = Task.Run(async () =>
        {
            await using (await asyncLock.LockAsync(cts.Token))
            {
                while (endLoopIndicator)
                {
                    await Task.Delay(50);
                }
            }
        }, TestContext.Current.CancellationToken);

        await Task.Delay(100, TestContext.Current.CancellationToken);
        var task2 = Task.Run(async () =>
        {
            try
            {
                await using (await asyncLock.LockAsync(cts.Token))
                {
                    endLoopIndicator = false;
                }
            }
            catch (OperationCanceledException)
            {
                endLoopIndicator = false;
            }
        }, TestContext.Current.CancellationToken);

        await Task.Delay(100, TestContext.Current.CancellationToken);
        await cts.CancelAsync();
        await task1;
        await task2;

        Assert.False(endLoopIndicator);
    }

    [Fact]
    public async Task Lock_ShouldQueueAwaiters()
    {
        // Arrange.
        var asyncLock = new AsyncLock();
        var list = new List<int>();
        var lockObj = new object();
        var @event = new ManualResetEventSlim();

        async Task LockAndAdd(int i)
        {
            var scope = await asyncLock.LockAsync();
            lock (lockObj)
            {
                list.Add(i);
            }
            await scope.DisposeAsync();
        }

        // Act.
        _ = Task.Run(async () =>
        {
            var scope = await asyncLock.LockAsync();
            @event.Wait();
            await scope.DisposeAsync();
        }, TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        var task1 = Task.Run(async () => await LockAndAdd(1), TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var task2 = Task.Run(async () => await LockAndAdd(2), TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var task3 = Task.Run(async () => await LockAndAdd(3), TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var task4 = Task.Run(async () => await LockAndAdd(4), TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var task5 = Task.Run(async () => await LockAndAdd(5), TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        @event.Set();
        await task1;
        await task2;
        await task3;
        await task4;
        await task5;

        // Assert.
        Assert.Equal([1, 2, 3, 4, 5], list);
    }

    [Fact]
    public async Task LockAsync_CanceledWhileWaiting_ShouldNotBreakMutualExclusion()
    {
        // Arrange.
        var asyncLock = new AsyncLock();
        var holderEntered = new SemaphoreSlim(0);
        var releaseHolder = new SemaphoreSlim(0);
        var cts = new CancellationTokenSource();

        // Act.
        var holderTask = Task.Run(async () =>
        {
            await using (await asyncLock.LockAsync())
            {
                holderEntered.Release();
                await releaseHolder.WaitAsync();
            }
        }, TestContext.Current.CancellationToken);
        await holderEntered.WaitAsync(TestContext.Current.CancellationToken);

        // The first attempt gets canceled while queued, the second one runs within the very
        // same execution flow and must still observe the lock as taken by the holder.
        var secondFlowTask = Task.Run(async () =>
        {
            try
            {
                await using (await asyncLock.LockAsync(cts.Token))
                {
                }
            }
            catch (OperationCanceledException)
            {
            }

            var retryTask = asyncLock.LockAsync().AsTask();
            var acquiredWhileHeld = await Task.WhenAny(retryTask, Task.Delay(500)) == retryTask;
            releaseHolder.Release();
            await using (await retryTask)
            {
            }
            return acquiredWhileHeld;
        });

        await Task.Delay(100, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        Assert.False(await secondFlowTask, "The lock was granted while another flow was holding it.");
        await holderTask;
    }

    [Fact]
    public async Task LockAsync_DisposedTwice_ShouldBeNoOp()
    {
        var asyncLock = new AsyncLock();

        var scope = await asyncLock.LockAsync(TestContext.Current.CancellationToken);
        await scope.DisposeAsync();
        await scope.DisposeAsync();

        Assert.False(asyncLock.IsTaken);
        await using (await asyncLock.LockAsync(TestContext.Current.CancellationToken))
        {
            Assert.True(asyncLock.IsTaken);
        }
    }
}
