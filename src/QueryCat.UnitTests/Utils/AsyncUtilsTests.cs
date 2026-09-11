using Xunit;
using Xunit.Abstractions;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="AsyncUtils" />.
/// </summary>
public class AsyncUtilsTests
{
    private ITestOutputHelper _output;

    public AsyncUtilsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RunSync_MultipleConcurrentRuns_ShouldRun()
    {
        // Arrange.
        const int iterations = 10;
        var counter = 0;
        var tasks = new List<Task>();

        // Act.
        for (var i = 0; i < iterations; i++)
        {
            var task = Task.Run(() =>
            {
                AsyncUtils.RunSync(async () =>
                {
                    await Task.Delay(Random.Shared.Next(20, 60));
                    Interlocked.Increment(ref counter);
                });
            });
            tasks.Add(task);
        }
        Task.WaitAll(tasks);

        // Assert.
        Assert.Equal(iterations, counter);
    }

    [Fact]
    public void RunSync_NestedCalls_ShouldNotLock()
    {
        // Act.
        int a = 0;
        AsyncUtils.RunSync(async () =>
        {
            a++;
            await Task.Delay(10);
            AsyncUtils.RunSync(async () =>
            {
                a++;
                await Task.Delay(10);
                AsyncUtils.RunSync(async () =>
                {
                    a++;
                    await Task.Delay(10);
                });
            });
        });

        // Assert.
        Assert.Equal(3, a);
    }

    [Fact]
    public void RunSync_NestedCallsWithException_ShouldNotLock()
    {
        // Act.
        int a = 0;
        AsyncUtils.RunSync(async () =>
        {
            a++;
            await Task.Delay(10);
            AsyncUtils.RunSync(async () =>
            {
                a++;
                await Task.Delay(10);
                try
                {
                    AsyncUtils.RunSync(async () =>
                    {
                        throw new Exception("test");
                        a++;
                        await Task.Delay(10);
                    });
                }
                catch (Exception e)
                {
                }
            });
        });

        // Assert.
        Assert.Equal(2, a);
    }

    [Fact]
    public async Task Post_FireAndForgetCallAfterRunSyncScope_ShouldProceedOrphan()
    {
        // Arrange.
        int a = 0;
        async Task FireAndForgetAsync()
        {
            var synchronizationContext = SynchronizationContext.Current;
            await Task.Delay(500);
            synchronizationContext!.Post(async _ =>
            {
                // It can run after RunSync scope.
                await Task.Delay(50);
                a++;
            }, null);
        }

        // Act.
        AsyncUtils.RunSync(async () =>
        {
            a++;
            await Task.Delay(10);
#pragma warning disable CS4014
            FireAndForgetAsync();
#pragma warning restore CS4014
        });
        await Task.Delay(1000);

        // Assert.
        Assert.Equal(2, a);
    }

    [Fact]
    public void RunSync_PostFromAbandonedWork_DoesNotEnterNextRun()
    {
        SynchronizationContext? firstRunContext = null;
        AsyncUtils.RunSync(async () =>
        {
            firstRunContext = SynchronizationContext.Current;
            await Task.Yield();
        });

        // The run is over and the context is back in the pool. This Post is what a
        // continuation abandoned by the first run does when it finally resumes:
        // a fire-and-forget task, the losing branch of Task.WhenAny, a cancelled but
        // not yet completed I/O, a late IAsyncDisposable.
        var abandonedRanOnThread = 0;
        var abandonedRan = new ManualResetEventSlim(false);
        firstRunContext!.Post(
            _ =>
            {
                abandonedRanOnThread = Environment.CurrentManagedThreadId;
                abandonedRan.Set();
            },
            null);

        var secondRunThread = 0;
        var value = AsyncUtils.RunSync(async () =>
        {
            secondRunThread = Environment.CurrentManagedThreadId;
            await Task.Yield();
            return 42;
        });

        abandonedRan.Wait(TimeSpan.FromSeconds(5));

        Assert.Equal(42, value);
        Assert.NotEqual(secondRunThread, abandonedRanOnThread);
    }
}
