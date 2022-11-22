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

    [Description("The function retrieves subfields such as year or hour from date/time values.")]
    [FunctionSignature("date_part(field: string, source: timestamp): integer")]
    public static VariantValue Extract(FunctionCallInfo args)
    {
        var field = args.GetAt(0).AsString.Trim().ToUpperInvariant();
        var source = args.GetAt(1);
        if (source.IsNull)
        {
            return VariantValue.Null;
        }
        var result = source.GetInternalType() switch
        {
            DataType.Timestamp => field switch
            {
                "YEAR" => source.AsTimestamp.Year,
                "MONTH" => source.AsTimestamp.Month,
                "DAY" => source.AsTimestamp.Day,
                "HOUR" => source.AsTimestamp.Hour,
                "MINUTE" => source.AsTimestamp.Minute,
                "SECOND" => source.AsTimestamp.Second,
                _ => throw new SemanticException("Incorrect part."),
            },
            DataType.Interval => field switch
            {
                "DAY" => source.AsInterval.Days,
                "HOUR" => source.AsInterval.Hours,
                "MINUTE" => source.AsInterval.Minutes,
                "SECOND" => source.AsInterval.Seconds,
                "MILLISECOND" => source.AsInterval.Milliseconds,
                _ => throw new SemanticException("Incorrect part."),
            },
            _ => throw new SemanticException(Resources.Errors.InvalidArgumentType),
        };
        return new VariantValue(result);
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ToDate);
        functionsManager.RegisterFunction(Now);
        functionsManager.RegisterFunction(Date);
        functionsManager.RegisterFunction(Extract);
    }
}
