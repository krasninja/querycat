using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using QueryCat.Backend.Types;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class VariantValueBenchmarks
{
    private readonly int _totalCount = 10_000;

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
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
    public VariantValue CreateListOfVariantValuesAndSumUsingDelegate()
    {
        var list = new List<VariantValue>(_totalCount);

        for (int i = 0; i < _totalCount; i++)
        {
            list.Add(new VariantValue(i));
        }

        var value = new VariantValue(0);
        var addAction = VariantValue.GetAddDelegate(value.GetInternalType(), value.GetInternalType());
        for (int i = 0; i < _totalCount; i++)
        {
            var right = list[i];
            value = addAction.Invoke(ref value, ref right);
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
