using BenchmarkDotNet.Attributes;
using DotNext.Buffers;
using DotNext.IO;
using QueryCat.Backend.Utils;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class DynamicBufferBenchmarks
{
    private readonly byte[] _chunk = new byte[2048];

    private readonly int _totalCount = 100_000;

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

        for (int remaining = _totalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, _chunk.Length);
            dynamicBuffer.Write(_chunk);
        }
    }

    [Benchmark]
    public void WriteToSparseBuffer()
    {
        var buffer = new SparseBufferWriter<byte>(4096, SparseBufferGrowth.Linear);
        var ms = buffer.AsStream(false);
        for (int remaining = _totalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, _chunk.Length);
            ms.Write(_chunk, 0, taken);
        }
    }
}
