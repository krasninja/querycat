using Xunit;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.UnitTests.Functions;

/// <summary>
/// Tests for <see cref="FunctionsManager" />.
/// </summary>
public sealed class FunctionsManagerTests
{
    private readonly FunctionsManager _functionsManager;

    public FunctionsManagerTests()
    {
        _functionsManager = new(ExecutionThread.DefaultInstance);
    }

    [Fact]
    public void RegisterFunction_SimpleFunction_ShouldEvalCorrectly()
    {
        // Act.
        _functionsManager.RegisterFunction(SumIntegers);

        var func = _functionsManager.FindByName("add",
            FunctionArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.Integer));
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(ExecutionThread.DefaultInstance,
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
            FunctionArgumentsTypes.FromPositionArguments(
                DataType.String, DataType.Integer, DataType.Integer));
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(ExecutionThread.DefaultInstance,
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
            FunctionArgumentsTypes.FromPositionArguments(DataType.String));
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(ExecutionThread.DefaultInstance, new VariantValue("sum"));
        var ret = func.Delegate(functionCallInfo);

        Assert.Equal("sum: 0", ret);
    }

    [FunctionSignature("add(str: string, ...nums: integer[]): integer")]
    private static VariantValue SumIntegersVariadic(FunctionCallInfo callInfo)
    {
        var str = callInfo.GetAt(0);
        long sum = 0;
        for (int i = 1; i < callInfo.Arguments.Values.Length; i++)
        {
            sum += callInfo.GetAt(i).AsInteger;
        }
        return new VariantValue($"{str}: {sum}");
    }

    [Fact]
    public void RegisterFunction_RegisterFromClass_ShouldEvalCorrectly()
    {
        // Arrange.
        _functionsManager.RegisterFromType<TestClass1>();

        // Act.
        var func1 = _functionsManager.FindByName("test_class1",
            FunctionArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value1 = ExecutionThread.DefaultInstance.RunFunction(func1.Delegate, 1, "2");
        var func2 = _functionsManager.FindByName("test_class2",
            FunctionArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value2 = ExecutionThread.DefaultInstance.RunFunction(func2.Delegate, 1, "2");
        var func3 = _functionsManager.FindByName("test_class3",
            FunctionArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value3 = ExecutionThread.DefaultInstance.RunFunction(func3.Delegate, 1, "2");

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
        _functionsManager.RegisterFromType<TestClass2>();

        // Act.
        var func1 = _functionsManager.FindByName("function1");
        ExecutionThread.DefaultInstance.RunFunction(func1.Delegate);
        var func2 = _functionsManager.FindByName("testfunc",
            FunctionArgumentsTypes.FromPositionArguments(DataType.String));
        var value2 = ExecutionThread.DefaultInstance.RunFunction(func2.Delegate, "2");

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
