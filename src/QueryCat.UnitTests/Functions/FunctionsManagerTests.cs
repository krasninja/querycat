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
    private readonly FunctionsManager _functionsManager = new();

    [Fact]
    public void RegisterFunction_SimpleFunction_ShouldEvalCorrectly()
    {
        // Act.
        _functionsManager.RegisterFunction(SumIntegers);

        var func = _functionsManager.FindByName("add",
            FunctionArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.Integer));
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(ExecutionThread.Empty,
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
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(ExecutionThread.Empty,
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
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(ExecutionThread.Empty, new VariantValue("sum"));
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
        var value1 = ExecutionThread.Empty.CallFunction(func1, 1, "2");
        var func2 = _functionsManager.FindByName("test_class2",
            FunctionArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value2 = ExecutionThread.Empty.CallFunction(func2, 1, "2");
        var func3 = _functionsManager.FindByName("test_class3",
            FunctionArgumentsTypes.FromPositionArguments(DataType.Integer, DataType.String));
        var value3 = ExecutionThread.Empty.CallFunction(func3, 1, "2");

        // Assert.
        Assert.Equal("1 2", value1.GetAsObject<TestClass1>().Value);
        Assert.Equal("1 2", value2.GetAsObject<TestClass1>().Value);
        Assert.Equal("1 2", value3.GetAsObject<TestClass1>().Value);
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
}
