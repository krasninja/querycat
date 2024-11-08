using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Math functions.
/// </summary>
internal static class MathFunctions
{
    [SafeFunction]
    [Description("Absolute value.")]
    [FunctionSignature("abs(x: integer): integer")]
    [FunctionSignature("abs(x: float): float")]
    [FunctionSignature("abs(x: numeric): numeric")]
    public static VariantValue Abs(IExecutionThread thread)
    {
        var x = thread.Stack.Pop();
        return x.Type switch
        {
            DataType.Float => new VariantValue(Math.Abs(x.AsFloatUnsafe)),
            DataType.Integer => new VariantValue(Math.Abs(x.AsIntegerUnsafe)),
            DataType.Numeric => new VariantValue(Math.Abs(x.AsNumericUnsafe)),
            _ => VariantValue.Null,
        };
    }

    [SafeFunction]
    [Description("Converts radians to degrees.")]
    [FunctionSignature("degrees(rad: integer): float")]
    [FunctionSignature("degrees(rad: float): float")]
    public static VariantValue Degrees(IExecutionThread thread)
    {
        var rad = thread.Stack.Pop();
        return new VariantValue(180d / Math.PI * rad);
    }

    [SafeFunction]
    [Description("\"Pi\" constant.")]
    [FunctionSignature("pi(): float")]
    public static VariantValue Pi(IExecutionThread thread)
    {
        return new VariantValue(Math.PI);
    }

    [SafeFunction]
    [Description("Converts degrees to radians.")]
    [FunctionSignature("radians(deg: integer): float")]
    [FunctionSignature("radians(deg: float): float")]
    public static VariantValue Radians(IExecutionThread thread)
    {
        var deg = thread.Stack.Pop();
        return new VariantValue(Math.PI / 180d * deg);
    }

    [SafeFunction]
    [Description("Rounds to nearest integer.")]
    [FunctionSignature("round(x: float): float")]
    [FunctionSignature("round(x: numeric): numeric")]
    public static VariantValue Round(IExecutionThread thread)
    {
        var x = thread.Stack.Pop();
        return x.Type switch
        {
            DataType.Float => new VariantValue(Math.Round(x.AsFloatUnsafe)),
            DataType.Numeric => new VariantValue(Math.Round(x.AsNumericUnsafe)),
            _ => VariantValue.Null,
        };
    }

    [SafeFunction]
    [Description("Square root.")]
    [FunctionSignature("sqrt(x: integer): float")]
    [FunctionSignature("sqrt(x: float): float")]
    public static VariantValue Sqrt(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Sqrt(x.Value) : null);
    }

    #region Trigonometric Functions

    [SafeFunction]
    [Description("Cosine, argument in radians.")]
    [FunctionSignature("cos(x: integer): float")]
    [FunctionSignature("cos(x: float): float")]
    public static VariantValue Cos(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Cos(x.Value) : null);
    }

    [SafeFunction]
    [Description("Inverse cosine, result in radians.")]
    [FunctionSignature("acos(x: integer): float")]
    [FunctionSignature("acos(x: float): float")]
    public static VariantValue Acos(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Acos(x.Value) : null);
    }

    [SafeFunction]
    [Description("Sine, argument in radians.")]
    [FunctionSignature("sin(x: integer): float")]
    [FunctionSignature("sin(x: float): float")]
    public static VariantValue Sin(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Sin(x.Value) : null);
    }

    [SafeFunction]
    [Description("Inverse sine, result in radians.")]
    [FunctionSignature("asin(x: integer): float")]
    [FunctionSignature("asin(x: float): float")]
    public static VariantValue Asin(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Asin(x.Value) : null);
    }

    [SafeFunction]
    [Description("Tangent, argument in radians.")]
    [FunctionSignature("tan(x: integer): float")]
    [FunctionSignature("tan(x: float): float")]
    public static VariantValue Tan(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Tan(x.Value) : null);
    }

    [SafeFunction]
    [Description("Inverse tangent, result in radians.")]
    [FunctionSignature("atan(x: integer): float")]
    [FunctionSignature("atan(x: float): float")]
    public static VariantValue Atan(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Atan(x.Value) : null);
    }

    #endregion

    [SafeFunction]
    [Description("a raised to the power of b.")]
    [FunctionSignature("power(a: integer, b: integer): integer")]
    [FunctionSignature("power(a: float, b: float): float")]
    [FunctionSignature("power(a: integer, b: float): float")]
    [FunctionSignature("power(a: float, b: integer): float")]
    public static VariantValue Power(IExecutionThread thread)
    {
        var a = thread.Stack[0].AsFloat;
        var b = thread.Stack[1].AsFloat;
        if (!a.HasValue || !b.HasValue)
        {
            return VariantValue.Null;
        }
        return new VariantValue(Math.Pow(a.Value, b.Value));
    }

    [SafeFunction]
    [Description("Returns a random value in the range 0.0 <= x < 1.0.")]
    [FunctionSignature("random(): float")]
    public static VariantValue Random(IExecutionThread thread)
    {
        return new VariantValue(System.Random.Shared.NextDouble());
    }

    [SafeFunction]
    [Description("Nearest integer less than or equal to argument.")]
    [FunctionSignature("floor(x: float): float")]
    public static VariantValue Floor(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Floor(x.Value) : null);
    }

    [SafeFunction]
    [Description("Nearest integer greater than or equal to argument (same as ceil).")]
    [FunctionSignature("ceiling(x: float): float")]
    public static VariantValue Ceiling(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Ceiling(x.Value) : null);
    }

    [SafeFunction]
    [Description("The function selects the largest value from a list of any number of values.")]
    [FunctionSignature("greatest(...args: any[]): any")]
    public static VariantValue Greatest(IExecutionThread thread)
    {
        var notNullArgs = thread.Stack.Where(v => !v.IsNull).ToArray();
        if (!notNullArgs.Any())
        {
            return VariantValue.Null;
        }
        var maxValue = notNullArgs.First();
        for (var i = 1; i < notNullArgs.Length; i++)
        {
            if (VariantValue.Greater(in notNullArgs[i], in maxValue, out _).AsBoolean)
            {
                maxValue = notNullArgs[i];
            }
        }
        return maxValue;
    }

    [SafeFunction]
    [Description("The function selects the least value from a list of any number of values.")]
    [FunctionSignature("least(...args: any[]): any")]
    public static VariantValue Least(IExecutionThread thread)
    {
        var notNullArgs = thread.Stack.Where(v => !v.IsNull).ToArray();
        if (!notNullArgs.Any())
        {
            return VariantValue.Null;
        }
        var maxValue = notNullArgs.First();
        for (var i = 1; i < notNullArgs.Length; i++)
        {
            if (VariantValue.Less(in notNullArgs[i], in maxValue, out _).AsBoolean)
            {
                maxValue = notNullArgs[i];
            }
        }
        return maxValue;
    }

    [SafeFunction]
    [Description("Natural logarithm.")]
    [FunctionSignature("ln(x: integer): float")]
    [FunctionSignature("ln(x: float): float")]
    public static VariantValue Ln(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Log(x.Value) : null);
    }

    [SafeFunction]
    [Description("Base 10 logarithm.")]
    [FunctionSignature("log(x: integer): float")]
    [FunctionSignature("log(x: float): float")]
    public static VariantValue Log(IExecutionThread thread)
    {
        var x = thread.Stack.Pop().AsFloat;
        return new VariantValue(x.HasValue ? Math.Log10(x.Value) : null);
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Abs);
        functionsManager.RegisterFunction(Degrees);
        functionsManager.RegisterFunction(Pi);
        functionsManager.RegisterFunction(Radians);
        functionsManager.RegisterFunction(Round);
        functionsManager.RegisterFunction(Sqrt);
        functionsManager.RegisterFunction(Cos);
        functionsManager.RegisterFunction(Acos);
        functionsManager.RegisterFunction(Sin);
        functionsManager.RegisterFunction(Asin);
        functionsManager.RegisterFunction(Tan);
        functionsManager.RegisterFunction(Atan);
        functionsManager.RegisterFunction(Power);
        functionsManager.RegisterFunction(Random);
        functionsManager.RegisterFunction(Floor);
        functionsManager.RegisterFunction(Ceiling);
        functionsManager.RegisterFunction(Greatest);
        functionsManager.RegisterFunction(Least);
        functionsManager.RegisterFunction(Ln);
        functionsManager.RegisterFunction(Log);
    }
}
