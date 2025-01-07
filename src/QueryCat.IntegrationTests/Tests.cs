using System.Globalization;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
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
    public async Task Select(string fileName)
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
        _testThread.TopScope.Variables["user3"] = VariantValue.CreateFromObject(User.GetTestUser3());

        var data = TestThread.GetQueryData(fileName);
        if (data.Skip)
        {
            SkipException.ForSkip(data.Comment);
            return;
        }
        var value = await _testThread.RunAsync(data.Query);

        // Get query plan.
        if (value.Type == DataType.Object
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
    internal static VariantValue FuncWithObject(IExecutionThread thread)
        => VariantValue.CreateFromObject(new object());

    [SafeFunction]
    [FunctionSignature("objret(): object")]
    internal static VariantValue ReturnObjFunc(IExecutionThread thread)
        => VariantValue.CreateFromObject(new object());

    [SafeFunction]
    [FunctionSignature("addopt(a: integer, b?: integer = 2): integer")]
    internal static VariantValue SumIntegersOpt(IExecutionThread thread)
    {
        var a = thread.Stack[0];
        var b = thread.Stack[1];
        return new VariantValue(a.AsInteger + b.AsInteger);
    }

    [SafeFunction]
    [FunctionSignature("add(a: integer, b: integer): integer")]
    internal static VariantValue SumIntegers(IExecutionThread thread)
    {
        return new VariantValue(thread.Stack[0].AsInteger
            + thread.Stack[1].AsInteger);
    }

    [SafeFunction]
    [FunctionSignature("void_func(a: integer): void")]
    internal static VariantValue VoidFunc(IExecutionThread thread) => VariantValue.Null;

    /// <inheritdoc />
    public void Dispose()
    {
        _testThread.Dispose();
    }
}
