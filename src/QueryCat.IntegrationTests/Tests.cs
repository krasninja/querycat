using Xunit;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests;

/// <summary>
/// Various SELECT query tests.
/// </summary>
public sealed class Tests : IDisposable
{
    private readonly IExecutionThread _testThread;

    public Tests()
    {
        _testThread = TestThread.CreateBootstrapper()
            .WithRegistrations(AdditionalRegistration.Register)
            .Create();
    }

    [Theory]
    [MemberData(nameof(GetData))]
    public void Select(string fileName)
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SumIntegers);
        _testThread.FunctionsManager.RegisterFunction(FuncWithObject);
        _testThread.FunctionsManager.RegisterFunction(ReturnObjFunc);
        _testThread.FunctionsManager.RegisterFunction(SumIntegersOpt);
        _testThread.FunctionsManager.RegisterFunction(VoidFunc);

        var data = TestThread.GetQueryData(fileName);
        _testThread.Run(data.Query);

        // Act.
        var result = TestThread.GetQueryResult(_testThread);

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
