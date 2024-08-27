using BenchmarkDotNet.Attributes;
using NCalc;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Benchmarks.Benchmarks;

/// <summary>
/// Test NCalc and QueryCat with Pi calc formula.
/// </summary>
/// <remarks>
/// https://en.wikipedia.org/wiki/Leibniz_formula_for_%CF%80.
/// </remarks>
[MemoryDiagnoser]
public class NCalcBenchmark
{
    private readonly IExecutionThread _executionThread;
    private readonly string _qcatQuery;
    private readonly string _ncalcQuery;

    public NCalcBenchmark()
    {
        _executionThread = new ExecutionThreadBootstrapper()
            .WithStandardFunctions()
            .Create();
        _qcatQuery = GenerateQuery("power", "sqrt");
        _ncalcQuery = GenerateQuery("Pow", "Sqrt");
    }

    [Benchmark]
    public VariantValue CalculatePiWithQueryCat()
    {
        var result = _executionThread.Run(_qcatQuery);
        return result;
    }

    [Benchmark]
    public double CalculatePiWithNCalc()
    {
        var result = new Expression(_ncalcQuery).Evaluate();
        return (double)result!;
    }

    private static string GenerateQuery(string powerFuncName, string sqrtFuncName)
    {
        var items = new List<string>();
        for (var i = 0; i < 20; i++)
        {
            items.Add($"{powerFuncName}(-1.0, {i})/(2*{i} + 1)");
        }
        return "(" + string.Join(" + ", items) + ") * 4";
    }
}
