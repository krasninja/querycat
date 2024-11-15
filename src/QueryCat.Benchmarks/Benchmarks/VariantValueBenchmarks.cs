using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using QueryCat.Backend.Core.Types;

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
        var addAction = VariantValue.GetAddDelegate(value.Type, value.Type);
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

    public static VariantValue SumVal(in VariantValue v1, in VariantValue v2, in VariantValue v3, in VariantValue v4, in VariantValue v5)
        => new(v1.AsFloatUnsafe + v2.AsFloatUnsafe + v3.AsFloatUnsafe + v4.AsFloatUnsafe + v5.AsFloatUnsafe);

    [Benchmark]
    public VariantValue PassByValue()
    {
        var arg1 = new VariantValue(10.53);
        var arg2 = new VariantValue(11.53);
        var arg3 = new VariantValue(12.53);
        var arg4 = new VariantValue(13.53);
        var arg5 = new VariantValue(14.53);
        var sum = new VariantValue(0.0);
        for (var i = 0; i < 1000; i++)
        {
            sum += SumVal(arg1, arg2, arg3, arg4, arg5);
        }
        return sum;
    }

    public static VariantValue SumRef(ref VariantValue v1, ref VariantValue v2, ref VariantValue v3, ref VariantValue v4, ref VariantValue v5)
        => new(v1.AsFloatUnsafe + v2.AsFloatUnsafe + v3.AsFloatUnsafe + v4.AsFloatUnsafe + v5.AsFloatUnsafe);

    [Benchmark]
    public VariantValue PassByRef()
    {
        var arg1 = new VariantValue(10.53);
        var arg2 = new VariantValue(11.53);
        var arg3 = new VariantValue(12.53);
        var arg4 = new VariantValue(13.53);
        var arg5 = new VariantValue(14.53);
        var sum = new VariantValue(0.0);
        for (var i = 0; i < 1000; i++)
        {
            sum += SumRef(ref arg1, ref arg2, ref arg3, ref arg4, ref arg5);
        }
        return sum;
    }
}
