using Xunit;
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
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(
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
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(
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
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(new VariantValue("sum"));
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
}
