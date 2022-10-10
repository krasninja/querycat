using BenchmarkDotNet.Attributes;
using QueryCat.Backend.Types;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class VariantValueBenchmarks
{
    private int _totalCount = 40_000_000;

    [Benchmark]
    public VariantValue CreateListOfVariantValuesAndSum()
    {
        var list = new List<VariantValue>(_totalCount);

        for (int i = 0; i < _totalCount; i++)
        {
            list.Add(new VariantValue(i));
        }

        var value = new VariantValue(0);
        for (int i = 0; i < _totalCount; i++)
        {
            value += list[i];
        }
        return value;
    }

    [Benchmark]
    public long? CreateListOfLongsAndSum()
    {
        var list = new List<long?>(_totalCount);

        for (int i = 0; i < _totalCount; i++)
        {
            list.Add(i);
        }

        long? value = 0;
        for (int i = 0; i < _totalCount; i++)
        {
            value += list[i];
        }
        return value;
    }
}
