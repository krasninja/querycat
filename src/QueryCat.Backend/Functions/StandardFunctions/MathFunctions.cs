using System.ComponentModel;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// Math functions.
/// </summary>
public static class MathFunctions
{
    [Description("Absolute value.")]
    [FunctionSignature("abs(x: integer): integer")]
    [FunctionSignature("abs(x: float): float")]
    [FunctionSignature("abs(x: numeric): numeric")]
    public static VariantValue Abs(FunctionCallInfo args)
    {
        var x = args.GetAt(0);
        return x.GetInternalType() switch
        {
            DataType.Float => new VariantValue(Math.Abs(x.AsFloat)),
            DataType.Integer => new VariantValue(Math.Abs(x.AsInteger)),
            DataType.Numeric => new VariantValue(Math.Abs(x.AsNumeric)),
            _ => VariantValue.Null
        };
    }

    [Description("Converts radians to degrees.")]
    [FunctionSignature("degrees(rad: integer): float")]
    [FunctionSignature("degrees(rad: float): float")]
    public static VariantValue Degrees(FunctionCallInfo args)
    {
        var rad = args.GetAt(0);
        return new VariantValue(180d / Math.PI * rad);
    }

    [Description("\"Pi\" constant.")]
    [FunctionSignature("pi(): float")]
    public static VariantValue Pi(FunctionCallInfo args)
    {
        return new VariantValue(Math.PI);
    }

    [Description("Converts degrees to radians.")]
    [FunctionSignature("radians(deg: integer): float")]
    [FunctionSignature("radians(deg: float): float")]
    public static VariantValue Radians(FunctionCallInfo args)
    {
        var deg = args.GetAt(0);
        return new VariantValue(Math.PI / 180d * deg);
    }

    [Description("Rounds to nearest integer.")]
    [FunctionSignature("round(x: integer): float")]
    [FunctionSignature("round(x: float): float")]
    public static VariantValue Round(FunctionCallInfo args)
    {
        var x = args.GetAt(0);
        return new VariantValue(Math.Round(x.AsFloat));
    }

    [Description("Square root.")]
    [FunctionSignature("sqrt(x: integer): float")]
    [FunctionSignature("sqrt(x: float): float")]
    public static VariantValue Sqrt(FunctionCallInfo args)
    {
        var x = args.GetAt(0);
        return new VariantValue(Math.Sqrt(x.AsFloat));
    }

    #region Trigonometric Functions

    [Description("Cosine, argument in radians.")]
    [FunctionSignature("cos(x: integer): float")]
    [FunctionSignature("cos(x: float): float")]
    public static VariantValue Cos(FunctionCallInfo args)
    {
        var x = args.GetAt(0);
        return new VariantValue(Math.Cos(x.AsFloat));
    }

    [Description("Sine, argument in radians.")]
    [FunctionSignature("sin(x: integer): float")]
    [FunctionSignature("sin(x: float): float")]
    public static VariantValue Sin(FunctionCallInfo args)
    {
        var x = args.GetAt(0);
        return new VariantValue(Math.Sin(x.AsFloat));
    }

    [Description("Tangent, argument in radians.")]
    [FunctionSignature("tan(x: integer): float")]
    [FunctionSignature("tan(x: float): float")]
    public static VariantValue Tan(FunctionCallInfo args)
    {
        var x = args.GetAt(0);
        return new VariantValue(Math.Tan(x.AsFloat));
    }

    #endregion

    [Description("a raised to the power of b.")]
    [FunctionSignature("power(a: integer, b: integer): integer")]
    [FunctionSignature("power(a: float, b: float): float")]
    [FunctionSignature("power(a: integer, b: float): float")]
    [FunctionSignature("power(a: float, b: integer): float")]
    public static VariantValue Power(FunctionCallInfo args)
    {
        var a = args.GetAt(0);
        var b = args.GetAt(1);
        return a.GetInternalType() switch
        {
            DataType.Float => new VariantValue(Math.Pow(a.AsFloat, b.AsFloat)),
            DataType.Integer => new VariantValue(Math.Pow(a.AsFloat, b.AsFloat)),
            _ => VariantValue.Null
        };
    }

    [Description("Returns a random value in the range 0.0 <= x < 1.0.")]
    [FunctionSignature("random(): float")]
    public static VariantValue Random(FunctionCallInfo args)
    {
        return new VariantValue(System.Random.Shared.NextDouble());
    }

    [Description("Nearest integer less than or equal to argument.")]
    [FunctionSignature("floor(x: float): float")]
    public static VariantValue Floor(FunctionCallInfo args)
    {
        var x = args.GetAt(0).AsFloat;
        return new VariantValue(Math.Floor(x));
    }

    [Description("Nearest integer greater than or equal to argument (same as ceil).")]
    [FunctionSignature("ceiling(x: float): float")]
    public static VariantValue Ceiling(FunctionCallInfo args)
    {
        var x = args.GetAt(0).AsFloat;
        return new VariantValue(Math.Ceiling(x));
    }

    [Description("The function selects the largest value from a list of any number of values.")]
    [FunctionSignature("greatest(...args: any[]): any")]
    public static VariantValue Greatest(FunctionCallInfo args)
    {
        var notNullArgs = args.Arguments.Values.Where(v => !v.IsNull).ToArray();
        if (!notNullArgs.Any())
        {
            return VariantValue.Null;
        }
        var maxValue = notNullArgs.First();
        for (var i = 1; i < notNullArgs.Length; i++)
        {
            if (VariantValue.Greater(ref notNullArgs[i], ref maxValue, out _))
            {
                maxValue = notNullArgs[i];
            }
        }
        return maxValue;
    }

    [Description("The function selects the least value from a list of any number of values.")]
    [FunctionSignature("least(...args: any[]): any")]
    public static VariantValue Least(FunctionCallInfo args)
    {
        var notNullArgs = args.Arguments.Values.Where(v => !v.IsNull).ToArray();
        if (!notNullArgs.Any())
        {
            return VariantValue.Null;
        }
        var maxValue = notNullArgs.First();
        for (var i = 1; i < notNullArgs.Length; i++)
        {
            if (VariantValue.Less(ref notNullArgs[i], ref maxValue, out _))
            {
                maxValue = notNullArgs[i];
            }
        }
        return maxValue;
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Abs);
        functionsManager.RegisterFunction(Degrees);
        functionsManager.RegisterFunction(Pi);
        functionsManager.RegisterFunction(Radians);
        functionsManager.RegisterFunction(Round);
        functionsManager.RegisterFunction(Sqrt);
        functionsManager.RegisterFunction(Cos);
        functionsManager.RegisterFunction(Sin);
        functionsManager.RegisterFunction(Tan);
        functionsManager.RegisterFunction(Power);
        functionsManager.RegisterFunction(Random);
        functionsManager.RegisterFunction(Floor);
        functionsManager.RegisterFunction(Ceiling);
        functionsManager.RegisterFunction(Greatest);
        functionsManager.RegisterFunction(Least);
    }
}
