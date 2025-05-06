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
        await using (await asyncLock.LockAsync())
        {
            await using (await asyncLock.LockAsync())
            {
                await Task.Delay(20);
                await using (await asyncLock.LockAsync())
                {
                    resource = 20;
                }
                await using (await asyncLock.LockAsync())
                {
                    resource = 30;
                }
            }
        }

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
        });
        await Task.Delay(100);
        var task2 = Task.Run(async () =>
        {
            await using (await asyncLock.LockAsync())
            {
                resource = 30;
            }
        });
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
            });
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
            });
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
        });
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
        });

        await Task.Delay(100);
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
        });

        await Task.Delay(100);
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
        });
        await Task.Delay(100);

        var task1 = Task.Run(async () => await LockAndAdd(1));
        await Task.Delay(100);
        var task2 = Task.Run(async () => await LockAndAdd(2));
        await Task.Delay(100);
        var task3 = Task.Run(async () => await LockAndAdd(3));
        await Task.Delay(100);
        var task4 = Task.Run(async () => await LockAndAdd(4));
        await Task.Delay(100);
        var task5 = Task.Run(async () => await LockAndAdd(5));
        await Task.Delay(100);

        @event.Set();
        await task1;
        await task2;
        await task3;
        await task4;
        await task5;

        // Assert.
        Assert.Equal([1, 2, 3, 4, 5], list);
    }
}
