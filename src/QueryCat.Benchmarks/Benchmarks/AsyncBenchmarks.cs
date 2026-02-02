using BenchmarkDotNet.Attributes;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class AsyncBenchmarks
{
    [Benchmark]
    public void RunSync()
    {
        for (var i = 0; i < 100; i++)
        {
            AsyncUtils.RunSync(async (ct) =>
            {
                await Task.Delay(1, ct);
                await Task.Delay(1, ct);
                await Task.Delay(1, ct);
            });
        }
    }
}
