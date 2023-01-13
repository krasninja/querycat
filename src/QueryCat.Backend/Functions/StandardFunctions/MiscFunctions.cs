using System.ComponentModel;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// Miscellaneous functions.
/// </summary>
internal static class MiscFunctions
{
    [Description("The function returns a null value if value1 equals value2; otherwise it returns value1.")]
    [FunctionSignature("[nullif](value1: any, value2: any): any")]
    public static VariantValue NullIf(FunctionCallInfo args)
    {
        var value1 = args.GetAt(0);
        var value2 = args.GetAt(1);
        if (VariantValue.Equals(in value1, in value2, out _))
        {
            return VariantValue.Null;
        }
        return value1;
    }

    [Description("Not operation. The function can be used to suppress output.")]
    [FunctionSignature("nop(...args: any[]): void")]
    public static VariantValue Nop(FunctionCallInfo args)
    {
        return VariantValue.Null;
    }

    [Description("The function returns a version 4 (random) UUID.")]
    [FunctionSignature("uuid(): string")]
    public static VariantValue GetRandomGuid(FunctionCallInfo args)
    {
        return new VariantValue(Guid.NewGuid().ToString("D"));
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(NullIf);
        functionsManager.RegisterFunction(Nop);
        functionsManager.RegisterFunction(GetRandomGuid);
    }
}
