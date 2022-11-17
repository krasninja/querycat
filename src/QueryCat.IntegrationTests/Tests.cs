using Xunit;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.IntegrationTests;

/// <summary>
/// Various SELECT query tests.
/// </summary>
public class Tests
{
    private readonly Backend.Tests.TestRunner _testRunner = new();

    [Theory]
    [MemberData(nameof(GetData))]
    public void Select(string fileName)
    {
        // Arrange.
        _testRunner.Bootstrap();
        _testRunner.ExecutionThread.FunctionsManager.RegisterFunction(SumIntegers);
        _testRunner.ExecutionThread.FunctionsManager.RegisterFunction(FuncWithObject);
        _testRunner.ExecutionThread.FunctionsManager.RegisterFunction(ReturnObjFunc);
        _testRunner.ExecutionThread.FunctionsManager.RegisterFunction(SumIntegersOpt);
        var data = _testRunner.GetQueryData(fileName);
        _testRunner.Run(data.Query);

        // Act.
        var result = _testRunner.GetQueryResult();

        // Assert.
        Assert.Equal(data.Expected, result);
    }

    public static IEnumerable<object[]> GetData() => Backend.Tests.TestRunner.GetTestFiles();

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
}
