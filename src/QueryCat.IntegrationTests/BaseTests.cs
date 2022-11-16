using System.Globalization;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;
using QueryCat.IntegrationTests.Internal;

namespace QueryCat.IntegrationTests;

/// <summary>
/// Base class for tests.
/// </summary>
public class BaseTests : IDisposable
{
    private readonly MemoryStream _memory = new();

    protected Runner Runner { get; }

    protected BaseTests()
    {
        Runner = new(new ExecutionOptions
        {
            DefaultRowsOutput = new DsvOutput(new DsvOptions(_memory)
            {
                HasHeader = false,
                InputOptions = new StreamRowsInputOptions
                {
                    DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
                    {
                        QuoteChars = new[] { '"' },
                        Delimiters = new[] { ',' },
                        BufferSize = 13,
                    },
                    AddInputSourceColumn = false,
                }
            }),
            UseConfig = true,
            AddRowNumberColumn = false,
        });
        Runner.Bootstrap();
        Runner.ExecutionThread.FunctionsManager.RegisterFunctionsFromAssemblies(
            typeof(DataProviders.Registration).Assembly);
        Runner.ExecutionThread.FunctionsManager.RegisterFunction(SumIntegers);
        Runner.ExecutionThread.FunctionsManager.RegisterFunction(FuncWithObject);
        Runner.ExecutionThread.FunctionsManager.RegisterFunction(ReturnObjFunc);
        Runner.ExecutionThread.FunctionsManager.RegisterFunction(SumIntegersOpt);
    }

    protected static string GetDataDirectory()
        => Path.Combine(TestParser.FindTestsDirectory(), "../Data");

    protected static string GetTestsDirectory()
        => Path.Combine(TestParser.FindTestsDirectory(), "../Tests");

    protected TestData PrepareTestData(string fileName)
    {
        Directory.SetCurrentDirectory(GetDataDirectory());
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        var testParser = new TestParser(fileName);
        return testParser.Parse();
    }

    protected string GetQueryResult()
    {
        _memory.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(_memory);
        return sr.ReadToEnd().Replace("\r\n", "\n").Trim();
    }

    [FunctionSignature("objfunc(a: integer, b: object): object")]
    internal static VariantValue FuncWithObject(FunctionCallInfo callInfo)
        => VariantValue.CreateFromObject(new object());

    [FunctionSignature("objret(): object")]
    internal static VariantValue ReturnObjFunc(FunctionCallInfo args)
        => VariantValue.CreateFromObject(new object());

    [FunctionSignature("addopt(a: integer, b?: integer = 2): integer")]
    internal static VariantValue SumIntegersOpt(FunctionCallInfo callInfo)
    {
        var a = callInfo.GetAt(0);
        var b = callInfo.GetAt(1);
        return new VariantValue(a.AsInteger + b.AsInteger);
    }

    [FunctionSignature("add(a: integer, b: integer): integer")]
    internal static VariantValue SumIntegers(FunctionCallInfo callInfo)
    {
        return new VariantValue(callInfo.GetAt(0).AsInteger
            + callInfo.GetAt(1).AsInteger);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _memory.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
