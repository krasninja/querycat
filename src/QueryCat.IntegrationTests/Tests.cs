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
public sealed class Tests
{
    private readonly ITestOutputHelper _output;
    private readonly ExecutionThreadBootstrapper _executionThreadBootstrapper = TestThread.CreateBootstrapper()
        .WithRegistrations(AdditionalRegistration.Register)
        .WithRegistrations(Backend.Addons.Functions.JsonFunctions.RegisterFunctions);

    public Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetData))]
    public async Task Select(string fileName)
    {
        // Arrange.
        await using var thread = await _executionThreadBootstrapper.CreateAsync();
        Application.Culture = CultureInfo.InvariantCulture;
        thread.FunctionsManager.RegisterFunction(SumIntegers);
        thread.FunctionsManager.RegisterFunction(FuncWithObject);
        thread.FunctionsManager.RegisterFunction(ReturnObjFunc);
        thread.FunctionsManager.RegisterFunction(SumIntegersOpt);
        thread.FunctionsManager.RegisterFunction(VoidFunc);
        thread.FunctionsManager.RegisterFunction(ItStocksRowsInput.ItStocks);

        thread.TopScope.Variables["user1"] = VariantValue.CreateFromObject(User.GetTestUser1());
        thread.TopScope.Variables["user2"] = VariantValue.CreateFromObject(User.GetTestUser2());
        thread.TopScope.Variables["user3"] = VariantValue.CreateFromObject(User.GetTestUser3());

        var data = TestThread.GetQueryData(fileName);
        if (data.Skip)
        {
            SkipException.ForSkip(data.Comment);
            return;
        }
        var value = await thread.RunAsync(data.Query);

        // Get query plan.
        if (value.Type == DataType.Object
            && value.AsObject is IRowsIterator rowsIterator)
        {
            var sb = new IndentedStringBuilder();
            rowsIterator.Explain(sb);
            _output.WriteLine(sb.ToString());
        }

        // Act.
        var result = TestThread.GetQueryResult(thread);

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
}
