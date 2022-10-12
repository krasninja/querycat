using System.ComponentModel;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// Miscellaneous functions.
/// </summary>
internal static class MiscFunctions
{
    [Description("Return first not NULL value.")]
    [FunctionSignature("coalesce(...args: any[]): any")]
    public static VariantValue Coalesce(FunctionCallInfo args)
    {
        foreach (var argument in args.Arguments.Values)
        {
            if (!argument.IsNull)
            {
                return argument;
            }
        }
        return VariantValue.Null;
    }

    [Description("Convert value to string according to the given format.")]
    [FunctionSignature("to_char(args: any, fmt?: string): string")]
    public static VariantValue ToChar(FunctionCallInfo args)
    {
        var arg = args.GetAt(0);
        var format = args.GetAt(1);
        return !string.IsNullOrEmpty(format)
            ? new VariantValue(arg.ToString(format))
            : new VariantValue(arg.ToString());
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Coalesce);
        functionsManager.RegisterFunction(ToChar);
    }
}
