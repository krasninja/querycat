using BenchmarkDotNet.Attributes;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class DynamicBufferBenchmarks
{
    private const int TotalCount = 100_000;

    private readonly byte[] _chunk = new byte[2048];

    public DynamicBufferBenchmarks()
    {
        for (int i = 0; i < _chunk.Length; i++)
        {
            _chunk[i] = (byte)(i % 255);
        }
    }

    [Benchmark]
    public void WriteToDynamicBuffer()
    {
        var dynamicBuffer = new DynamicBuffer<byte>(chunkSize: 4096);

        for (int remaining = TotalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, _chunk.Length);
            dynamicBuffer.Write(_chunk);
        }
    }
}
