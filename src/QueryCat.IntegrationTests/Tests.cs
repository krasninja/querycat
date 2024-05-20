using System.Globalization;
using Xunit;
using Xunit.Abstractions;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Addons.Formatters;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.IntegrationTests.DataClasses;
using QueryCat.IntegrationTests.Inputs;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests;

/// <summary>
/// Various SELECT query tests.
/// </summary>
public sealed class Tests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IExecutionThread<ExecutionOptions> _testThread;

    public Tests(ITestOutputHelper output)
    {
        _output = output;
        _testThread = TestThread.CreateBootstrapper()
            .WithRegistrations(AdditionalRegistration.Register)
            .WithRegistrations(Backend.Addons.Functions.JsonFunctions.RegisterFunctions)
            .Create();
    }

    [Theory]
    [MemberData(nameof(GetData))]
    public void Select(string fileName)
    {
        // Arrange.
        Application.Culture = CultureInfo.InvariantCulture;
        _testThread.FunctionsManager.RegisterFunction(SumIntegers);
        _testThread.FunctionsManager.RegisterFunction(FuncWithObject);
        _testThread.FunctionsManager.RegisterFunction(ReturnObjFunc);
        _testThread.FunctionsManager.RegisterFunction(SumIntegersOpt);
        _testThread.FunctionsManager.RegisterFunction(VoidFunc);
        _testThread.FunctionsManager.RegisterFunction(ItStocksRowsInput.ItStocks);

        _testThread.TopScope.Variables["user1"] = VariantValue.CreateFromObject(User.GetTestUser1());
        _testThread.TopScope.Variables["user2"] = VariantValue.CreateFromObject(User.GetTestUser2());

        var data = TestThread.GetQueryData(fileName);
        var value = _testThread.Run(data.Query);

        // Get query plan.
        if (value.GetInternalType() == DataType.Object
            && value.AsObject is IRowsIterator rowsIterator)
        {
            var sb = new IndentedStringBuilder();
            rowsIterator.Explain(sb);
            _output.WriteLine(sb.ToString());
        }

        // Act.
        var result = TestThread.GetQueryResult(_testThread);

        // Assert.
        Assert.Equal(data.Expected, result);
    }

    public static IEnumerable<object[]> GetData() => TestThread.GetTestFiles();

    [SafeFunction]
    [FunctionSignature("objfunc(a: integer, b: object): object")]
    internal static VariantValue FuncWithObject(FunctionCallInfo callInfo)
        => VariantValue.CreateFromObject(new object());

    [SafeFunction]
    [FunctionSignature("objret(): object")]
    internal static VariantValue ReturnObjFunc(FunctionCallInfo args)
        => VariantValue.CreateFromObject(new object());

    [SafeFunction]
    [FunctionSignature("addopt(a: integer, b?: integer = 2): integer")]
    internal static VariantValue SumIntegersOpt(FunctionCallInfo callInfo)
    {
        var a = callInfo.GetAt(0);
        var b = callInfo.GetAt(1);
        return new VariantValue(a.AsInteger + b.AsInteger);
    }

    [SafeFunction]
    [FunctionSignature("add(a: integer, b: integer): integer")]
    internal static VariantValue SumIntegers(FunctionCallInfo callInfo)
    {
        return new VariantValue(callInfo.GetAt(0).AsInteger
            + callInfo.GetAt(1).AsInteger);
    }

    [SafeFunction]
    [FunctionSignature("void_func(a: integer): void")]
    internal static VariantValue VoidFunc(FunctionCallInfo callInfo) => VariantValue.Null;

    /// <inheritdoc />
    public void Dispose()
    {
        _testThread.Dispose();
    }
}
