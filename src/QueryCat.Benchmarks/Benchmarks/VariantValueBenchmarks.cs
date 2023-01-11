using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using QueryCat.Backend.Types;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class VariantValueBenchmarks
{
    private const int TotalCount = 10_000;

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public VariantValue CreateListOfVariantValuesAndSum()
    {
        var list = new List<VariantValue>(TotalCount);

        for (int i = 0; i < TotalCount; i++)
        {
            list.Add(new VariantValue(i));
        }

        var value = new VariantValue(0);
        for (int i = 0; i < TotalCount; i++)
        {
            value += list[i];
        }
        return value;
    }

    [Benchmark]
    public VariantValue CreateListOfVariantValuesAndSumUsingDelegate()
    {
        var list = new List<VariantValue>(TotalCount);

        for (int i = 0; i < TotalCount; i++)
        {
            list.Add(new VariantValue(i));
        }

        var value = new VariantValue(0);
        var addAction = VariantValue.GetAddDelegate(value.GetInternalType(), value.GetInternalType());
        for (int i = 0; i < TotalCount; i++)
        {
            var right = list[i];
            value = addAction.Invoke(in value, in right);
        }
        return value;
    }

    [Benchmark]
    public long? CreateListOfLongsAndSum()
    {
        var list = new List<long?>(TotalCount);

        for (int i = 0; i < TotalCount; i++)
        {
            list.Add(i);
        }

        long? value = 0;
        for (int i = 0; i < TotalCount; i++)
        {
            value += list[i];
        }
        return value;
    }
}
