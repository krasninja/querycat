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

    [Description("Current date and time")]
    [FunctionSignature("now(): timestamp")]
    public static VariantValue Now(FunctionCallInfo args)
    {
        return new VariantValue(DateTime.Now);
    }

    [Description("Takes the date part.")]
    [FunctionSignature("date(datetime: timestamp): timestamp")]
    public static VariantValue Date(FunctionCallInfo args)
    {
        return new VariantValue(args.GetAt(0).AsTimestamp.Date);
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ToDate);
        functionsManager.RegisterFunction(Now);
        functionsManager.RegisterFunction(Date);
    }
}
