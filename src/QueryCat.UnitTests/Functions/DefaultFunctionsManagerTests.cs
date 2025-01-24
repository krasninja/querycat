using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.FunctionsManager;
using QueryCat.Backend.Parser;

namespace QueryCat.UnitTests.Functions;

/// <summary>
/// Tests for <see cref="DefaultFunctionsManager" />.
/// </summary>
public sealed class DefaultFunctionsManagerTests
{
    private readonly DefaultFunctionsManager _functionsManager;

    public DefaultFunctionsManagerTests()
    {
        _functionsManager = new DefaultFunctionsManager(new AstBuilder());
    }

    [Fact]
    public async Task RegisterFunction_SimpleFunction_ShouldEvalCorrectly()
    {
        // Act.
        _functionsManager.RegisterFunction(SumIntegers);

        var func = _functionsManager.FindByNameFirst("add",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.Integer));
        using var frame = Executor.Thread.Stack.CreateFrameWithArguments(new VariantValue(5), new VariantValue(6));
        var ret = await FunctionCaller.CallAsync(func.Delegate, Executor.Thread);

        Assert.Equal(11L, ret.AsIntegerUnsafe);
    }

    [FunctionSignature("add(a: integer, b: integer, c: boolean = false): integer")]
    private static VariantValue SumIntegers(IExecutionThread thread)
    {
        return new VariantValue(thread.Stack[0].AsInteger + thread.Stack[1].AsInteger);
    }

    [Fact]
    public async Task RegisterFunction_VariadicArgumentMethod_ShouldEvalCorrectly()
    {
        // Act.
        _functionsManager.RegisterFunction(SumIntegersVariadic);

        var func = _functionsManager.FindByNameFirst("add",
            FunctionCallArgumentsTypes.FromPositionArguments(
                DataType.String, DataType.Integer, DataType.Integer));
        using var frame = Executor.Thread.Stack.CreateFrameWithArguments(
            new VariantValue("sum"), new VariantValue(5), new VariantValue(5));
        var ret = await FunctionCaller.CallAsync(func.Delegate, Executor.Thread);

        Assert.Equal("sum: 10", ret);
    }

    [Fact]
    public async Task RegisterFunction_VariadicArgumentMethodEmpty_ShouldEvalCorrectly()
    {
        // Act.
        _functionsManager.RegisterFunction(SumIntegersVariadic);

        var func = _functionsManager.FindByNameFirst("add",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.String));
        using var frame = Executor.Thread.Stack.CreateFrameWithArguments(new VariantValue("sum"));
        var ret = await FunctionCaller.CallAsync(func.Delegate, Executor.Thread);

        Assert.Equal("sum: 0", ret);
    }

    [FunctionSignature("add(str: string, ...nums: integer[]): integer")]
    private static VariantValue SumIntegersVariadic(IExecutionThread thread)
    {
        var str = thread.Stack[0];
        long? sum = 0;
        for (int i = 1; i < thread.Stack.FrameLength; i++)
        {
            sum += thread.Stack[i].AsInteger;
        }
        return new VariantValue($"{str}: {sum}");
    }

    [Fact]
    public async Task RegisterFunction_RegisterFromClass_ShouldEvalCorrectly()
    {
        // Arrange.
        var functions = _functionsManager.Factory.CreateFromType(typeof(TestClass1));
        _functionsManager.RegisterFunctions(functions);

        // Act.
        var func1 = _functionsManager.FindByNameFirst("test_class1",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value1 = await FunctionCaller.CallWithArgumentsAsync(func1.Delegate, Executor.Thread, [1, "2"]);
        var func2 = _functionsManager.FindByNameFirst("test_class2",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value2 = await FunctionCaller.CallWithArgumentsAsync(func2.Delegate, Executor.Thread, [1, "2"]);
        var func3 = _functionsManager.FindByNameFirst("test_class3",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value3 = await FunctionCaller.CallWithArgumentsAsync(func3.Delegate, Executor.Thread, [1, "2"]);

        // Assert.
        Assert.Equal("1 2", value1.As<TestClass1>()!.Value);
        Assert.Equal("1 2", value2.As<TestClass1>()!.Value);
        Assert.Equal("1 2", value3.As<TestClass1>()!.Value);
    }

    [FunctionSignature]
    [FunctionSignature("test_class2")]
    [FunctionSignature("test_class3(a: integer, b: string): object")]
    private class TestClass1
    {
        public string Value { get; }

        public TestClass1(int a, string str)
        {
            Value = $"{a} {str}";
        }
    }

    [Fact]
    public async Task RegisterFunction_RegisterFromType_ShouldEvalCorrectly()
    {
        // Arrange.
        var functions = _functionsManager.Factory.CreateFromType(typeof(TestClass2));
        _functionsManager.RegisterFunctions(functions);

        // Act.
        var func1 = _functionsManager.FindByNameFirst("function1");
        await FunctionCaller.CallWithArgumentsAsync(func1.Delegate, Executor.Thread);
        var func2 = _functionsManager.FindByNameFirst("testfunc",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.String));
        var value2 = await FunctionCaller.CallWithArgumentsAsync(func2.Delegate, Executor.Thread, ["2"]);

        // Assert.
        Assert.Equal(2, value2.AsInteger);
    }

    private class TestClass2
    {
        [FunctionSignature]
        public static void Function1()
        {
        }

        [FunctionSignature("testfunc")]
        public static int Function2(string str)
        {
            return int.Parse(str);
        }
    }
}
