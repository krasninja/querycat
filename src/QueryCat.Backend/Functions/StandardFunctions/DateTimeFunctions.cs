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
                "YEAR" or "Y" => source.AsTimestamp.Year,
                "DOY" => source.AsTimestamp.DayOfYear,
                "DAYOFYEAR" => source.AsTimestamp.DayOfYear,
                "MONTH" => source.AsTimestamp.Month,
                "DOW" => (int)source.AsTimestamp.DayOfWeek,
                "WEEKDAY" => (int)source.AsTimestamp.DayOfWeek,
                "DAY" or "D" => source.AsTimestamp.Day,
                "HOUR" or "H" => source.AsTimestamp.Hour,
                "MINUTE" or "MIN" or "M" => source.AsTimestamp.Minute,
                "SECOND" or "SEC" or "S" => source.AsTimestamp.Second,
                _ => throw new SemanticException("Incorrect field."),
            },
            DataType.Interval => field switch
            {
                "DAY" or "D" => source.AsInterval.Days,
                "HOUR" or "H" => source.AsInterval.Hours,
                "MINUTE" or "MIN" or "M" => source.AsInterval.Minutes,
                "SECOND" or "SEC" or "S" => source.AsInterval.Seconds,
                "MILLISECOND" or "MS" => source.AsInterval.Milliseconds,
                _ => throw new SemanticException("Incorrect field."),
            },
            _ => throw new SemanticException("Invalid argument type."),
        };
        return new VariantValue(result);
    }

    [Description("The function rounds or truncates a timestamp or interval to the date part you need.")]
    [FunctionSignature("date_trunc(field: string, source: timestamp): timestamp")]
    [FunctionSignature("date_trunc(field: string, source: interval): interval")]
    public static VariantValue Trunc(FunctionCallInfo args)
    {
        var field = args.GetAt(0).AsString.Trim().ToUpperInvariant();
        var source = args.GetAt(1);
        if (source.IsNull)
        {
            return VariantValue.Null;
        }
        var type = source.GetInternalType();
        if (type == DataType.Timestamp)
        {
            var target = source.AsTimestamp;
            var timestamp = field switch
            {
                "YEAR" or "Y" => new DateTime(target.Year, 1, 1, 0, 0, 0, target.Kind),
                "DAY" or "D" => new DateTime(target.Year, target.Month, target.Day, 0, 0, 0, target.Kind),
                "HOUR" or "H" => new DateTime(target.Year, target.Month, target.Day, target.Hour, 0, 0, target.Kind),
                "MINUTE" or "MIN" or "M" =>
                    new DateTime(target.Year, target.Month, target.Day, target.Hour, target.Minute, 0, target.Kind),
                "SECOND" or "SEC" or "S" =>
                    new DateTime(target.Year, target.Month, target.Day, target.Hour, target.Minute, target.Second, target.Kind),
                _ => throw new SemanticException("Incorrect field."),
            };
            return new VariantValue(timestamp);
        }
        else if (type == DataType.Interval)
        {
            var target = source.AsInterval;
            var interval = field switch
            {
                "DAY" or "D" => new TimeSpan(target.Days, 0, 0, 0),
                "HOUR" or "H" => new TimeSpan(target.Days, target.Hours, 0),
                "MINUTE" or "MIN" or "M" => new TimeSpan(target.Days, target.Hours, target.Minutes, 0),
                "SECOND" or "SEC" or "S" =>
                    new TimeSpan(target.Days, target.Hours, target.Minutes, target.Seconds, 0),
                _ => throw new SemanticException("Incorrect field."),
            };
            return new VariantValue(interval);
        }
        throw new SemanticException("Invalid argument type.");
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ToDate);
        functionsManager.RegisterFunction(Now);
        functionsManager.RegisterFunction(Date);
        functionsManager.RegisterFunction(Extract);
        functionsManager.RegisterFunction(Trunc);
    }
}
