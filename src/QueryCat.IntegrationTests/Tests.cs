using Xunit;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Tests;
using QueryCat.Backend.Types;

namespace QueryCat.IntegrationTests;

/// <summary>
/// Various SELECT query tests.
/// </summary>
public sealed class Tests : IDisposable
{
    private readonly TestThread _testThread = new();

    [Theory]
    [MemberData(nameof(GetData))]
    public void Select(string fileName)
    {
        // Arrange.
        new ExecutionThreadBootstrapper().Bootstrap(_testThread);
        _testThread.FunctionsManager.RegisterFunction(SumIntegers);
        _testThread.FunctionsManager.RegisterFunction(FuncWithObject);
        _testThread.FunctionsManager.RegisterFunction(ReturnObjFunc);
        _testThread.FunctionsManager.RegisterFunction(SumIntegersOpt);
        _testThread.FunctionsManager.RegisterFunction(VoidFunc);

        var data = TestThread.GetQueryData(fileName);
        _testThread.Run(data.Query);

        // Act.
        var result = _testThread.GetQueryResult();

        // Assert.
        Assert.Equal(data.Expected, result);
    }

    public static IEnumerable<object[]> GetData() => TestThread.GetTestFiles();

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

    [FunctionSignature("void_func(a: integer): void")]
    internal static VariantValue VoidFunc(FunctionCallInfo callInfo) => VariantValue.Null;

    /// <inheritdoc />
    public void Dispose()
    {
        _testThread.Dispose();
    }
}
