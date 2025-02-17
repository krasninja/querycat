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
}
