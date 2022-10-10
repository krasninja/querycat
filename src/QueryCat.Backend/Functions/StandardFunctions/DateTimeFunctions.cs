using System.ComponentModel;
using System.Globalization;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// Date time functions.
/// </summary>
public static class DateTimeFunctions
{
    [Description("Converts string to date according to the given format.")]
    [FunctionSignature("to_date(target: string, fmt: string): timestamp")]
    public static VariantValue ToDate(FunctionCallInfo args)
    {
        var target = args.GetAt(0).AsString;
        var format = args.GetAt(1).AsString;
        return new VariantValue(DateTime.ParseExact(target, format, CultureInfo.InvariantCulture));
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ToDate);
    }
}
