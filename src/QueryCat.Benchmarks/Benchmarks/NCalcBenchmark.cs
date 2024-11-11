using System.Data;
using System.Text;
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
public sealed class NCalcBenchmark : IDisposable
{
    private const string LogicalExpression =
        "(1089 = (1000 + 89)) AND 13 IN (1,2,3,4,5,6,7,8,9,10,11,12,13,14,15) AND 'INSERT' = 'INSERT'";

    private readonly IExecutionThread _executionThread;
    private readonly DataTable _dataTable;
    private readonly string _qcatQuery;
    private readonly string _ncalcQuery;

    public NCalcBenchmark()
    {
        _executionThread = new ExecutionThreadBootstrapper()
            .WithStandardFunctions()
            .Create();
        _dataTable = new DataTable();
        _qcatQuery = GenerateQuery("power", "sqrt");
        _ncalcQuery = GenerateQuery("Pow", "Sqrt");
    }

    #region Pi

    [Benchmark]
    public VariantValue CalculatePiWithQueryCat()
    {
        var result = _executionThread.Run(_qcatQuery);
        return result;
    }

    [Benchmark]
    public double CalculatePiWithNCalc()
    {
        var expression = new Expression(_ncalcQuery)
        {
            Options = ExpressionOptions.NoCache | ExpressionOptions.IgnoreCaseAtBuiltInFunctions,
        };
        var result = expression.Evaluate();
        return (double)result!;
    }

    #endregion

    #region Logical Expression

    [Benchmark]
    public VariantValue CalculateLogicalWithQueryCat()
    {
        var result = _executionThread.Run(LogicalExpression);
        return result;
    }

    [Benchmark]
    public object? CalculateLogicalWithNCalc()
    {
        var expression = new Expression(LogicalExpression)
        {
            Options = ExpressionOptions.NoCache | ExpressionOptions.IgnoreCaseAtBuiltInFunctions,
        };
        return expression.Evaluate();
    }

    [Benchmark]
    public object CalculateLogicalWithDataTable()
    {
        var result = _dataTable.Compute(LogicalExpression, string.Empty);
        return result;
    }

    #endregion

    private static string GenerateQuery(string powerFuncName, string sqrtFuncName)
    {
        const int count = 20;
        var sb = new StringBuilder(capacity: count * 20);
        sb.Append("(");
        for (var i = 0; i < count; i++)
        {
            sb.Append($"{powerFuncName}(-1.0, {i})/(2*{i} + 1)");
            if (i != count - 1)
            {
                sb.Append(" + ");
            }
        }
        sb.Append(") * 4");
        return sb.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dataTable.Dispose();
        _executionThread.Dispose();
    }
}
