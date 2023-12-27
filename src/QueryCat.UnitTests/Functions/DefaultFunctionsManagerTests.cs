using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Core;
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
    public void RegisterFunction_SimpleFunction_ShouldEvalCorrectly()
    {
        // Act.
        _functionsManager.RegisterFunction(SumIntegers);

        var func = _functionsManager.FindByName("add",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.Integer));
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(Executor.Thread,
            new VariantValue(5), new VariantValue(6));
        var ret = func.Delegate(functionCallInfo);

        Assert.Equal(11L, ret);
    }

    [FunctionSignature("add(a: integer, b: integer, c: boolean = false): integer")]
    private static VariantValue SumIntegers(FunctionCallInfo callInfo)
    {
        return new VariantValue(callInfo.GetAt(0).AsInteger + callInfo.GetAt(1).AsInteger);
    }

    [Fact]
    public void RegisterFunction_VariadicArgumentMethod_ShouldEvalCorrectly()
    {
        // Act.
        _functionsManager.RegisterFunction(SumIntegersVariadic);

        var func = _functionsManager.FindByName("add",
            FunctionCallArgumentsTypes.FromPositionArguments(
                DataType.String, DataType.Integer, DataType.Integer));
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(Executor.Thread,
            new VariantValue("sum"), new VariantValue(5), new VariantValue(5));
        var ret = func.Delegate(functionCallInfo);

        Assert.Equal("sum: 10", ret);
    }

    [Fact]
    public void RegisterFunction_VariadicArgumentMethodEmpty_ShouldEvalCorrectly()
    {
        // Act.
        _functionsManager.RegisterFunction(SumIntegersVariadic);

        var func = _functionsManager.FindByName("add",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.String));
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(Executor.Thread, new VariantValue("sum"));
        var ret = func.Delegate(functionCallInfo);

        Assert.Equal("sum: 0", ret);
    }

    [FunctionSignature("add(str: string, ...nums: integer[]): integer")]
    private static VariantValue SumIntegersVariadic(FunctionCallInfo callInfo)
    {
        var str = callInfo.GetAt(0);
        long sum = 0;
        for (int i = 1; i < callInfo.Count; i++)
        {
            sum += callInfo.GetAt(i).AsInteger;
        }
        return new VariantValue($"{str}: {sum}");
    }

    [Fact]
    public void RegisterFunction_RegisterFromClass_ShouldEvalCorrectly()
    {
        // Arrange.
        _functionsManager.RegisterFromType(typeof(TestClass1));

        // Act.
        var func1 = _functionsManager.FindByName("test_class1",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value1 = Executor.Thread.CallFunction(func1.Delegate, 1, "2");
        var func2 = _functionsManager.FindByName("test_class2",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value2 = Executor.Thread.CallFunction(func2.Delegate, 1, "2");
        var func3 = _functionsManager.FindByName("test_class3",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value3 = Executor.Thread.CallFunction(func3.Delegate, 1, "2");

        // Assert.
        Assert.Equal("1 2", value1.As<TestClass1>().Value);
        Assert.Equal("1 2", value2.As<TestClass1>().Value);
        Assert.Equal("1 2", value3.As<TestClass1>().Value);
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
    public void RegisterFunction_RegisterFromType_ShouldEvalCorrectly()
    {
        // Arrange.
        _functionsManager.RegisterFromType(typeof(TestClass2));

        // Act.
        var func1 = _functionsManager.FindByName("function1");
        Executor.Thread.CallFunction(func1.Delegate);
        var func2 = _functionsManager.FindByName("testfunc",
            FunctionCallArgumentsTypes.FromPositionArguments(DataType.String));
        var value2 = Executor.Thread.CallFunction(func2.Delegate, "2");

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
