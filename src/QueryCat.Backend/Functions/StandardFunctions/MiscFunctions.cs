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

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Coalesce);
    }
}
