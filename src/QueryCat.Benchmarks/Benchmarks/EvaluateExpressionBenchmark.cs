using BenchmarkDotNet.Attributes;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class EvaluateExpressionBenchmark
{
    private sealed class DataItem
    {
        public decimal Temperature { get; }

        public DateOnly Date { get; }

        public DataItem(decimal temperature, DateOnly date)
        {
            Temperature = temperature;
            Date = date;
        }
    }

    private readonly Dictionary<string, DataItem> _data = new()
    {
        ["KRAS"] = new DataItem(+6, new DateOnly(2024, 4, 25)),
        ["NSK"] = new DataItem(+7, new DateOnly(2024, 4, 25)),
        ["MSK"] = new DataItem(+16, new DateOnly(2024, 4, 25)),
        ["SPB"] = new DataItem(+5, new DateOnly(2024, 4, 25)),
    };

    private readonly IExecutionThread _executionThread = new ExecutionThreadBootstrapper()
        .WithStandardFunctions()
        .Create();

    [Benchmark]
    public void RunExpressionSeveralTimes()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = _executionThread.Run(
                """
                abs((data['KRAS'].Temperature +
                    data['NSK'].Temperature +
                    data['MSK'].Temperature +
                    data['SPB'].Temperature)::float / total)
                """,
                new Dictionary<string, VariantValue>
                {
                    ["data"] = VariantValue.CreateFromObject(_data),
                    ["total"] = new VariantValue(4),
                });
        }
    }
}
