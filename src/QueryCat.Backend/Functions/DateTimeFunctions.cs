using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Date time functions.
/// </summary>
internal static class DateTimeFunctions
{
    [SafeFunction]
    [Description("Converts string to date according to the given format.")]
    [FunctionSignature("to_date(target: string, fmt: string): timestamp")]
    public static VariantValue ToDate(IExecutionThread thread)
    {
        var target = thread.Stack[0].AsString;
        var format = thread.Stack[1].AsString;
        return new VariantValue(DateTime.ParseExact(target, format, Application.Culture));
    }

    [SafeFunction]
    [Description("Current date and time")]
    [FunctionSignature("now(): timestamp")]
    public static VariantValue Now(IExecutionThread thread)
    {
        return new VariantValue(DateTime.Now);
    }

    [SafeFunction]
    [Description("Takes the date part.")]
    [FunctionSignature("date(datetime: timestamp): timestamp")]
    public static VariantValue Date(IExecutionThread thread)
    {
        var ts = thread.Stack.Pop().AsTimestamp;
        return new VariantValue(ts?.Date);
    }

    [SafeFunction]
    [Description("The function retrieves subfields such as year or hour from date/time values.")]
    [FunctionSignature("date_part(field: string, source: timestamp): integer")]
    public static VariantValue Extract(IExecutionThread thread)
    {
        var field = thread.Stack[0].AsString.Trim().ToUpperInvariant();
        var source = thread.Stack[1];
        if (source.IsNull)
        {
            return VariantValue.Null;
        }
        var result = source.Type switch
        {
            DataType.Timestamp => field switch
            {
                "YEAR" or "Y" => source.AsTimestampUnsafe.Year,
                "DOY" => source.AsTimestampUnsafe.DayOfYear,
                "DAYOFYEAR" => source.AsTimestampUnsafe.DayOfYear,
                "MONTH" => source.AsTimestampUnsafe.Month,
                "DOW" => (int)source.AsTimestampUnsafe.DayOfWeek,
                "WEEKDAY" => (int)source.AsTimestampUnsafe.DayOfWeek,
                "DAY" or "D" => source.AsTimestampUnsafe.Day,
                "HOUR" or "H" => source.AsTimestampUnsafe.Hour,
                "MINUTE" or "MIN" or "M" => source.AsTimestampUnsafe.Minute,
                "SECOND" or "SEC" or "S" => source.AsTimestampUnsafe.Second,
                _ => throw new SemanticException(string.Format(Resources.Errors.InvalidField, field)),
            },
            DataType.Interval => field switch
            {
                "DAY" or "D" => source.AsIntervalUnsafe.Days,
                "HOUR" or "H" => source.AsIntervalUnsafe.Hours,
                "MINUTE" or "MIN" or "M" => source.AsIntervalUnsafe.Minutes,
                "SECOND" or "SEC" or "S" => source.AsIntervalUnsafe.Seconds,
                "MILLISECOND" or "MS" => source.AsIntervalUnsafe.Milliseconds,
                _ => throw new SemanticException(string.Format(Resources.Errors.InvalidField, field)),
            },
            _ => throw new SemanticException(Resources.Errors.InvalidArgumentType),
        };
        return new VariantValue(result);
    }

    [SafeFunction]
    [Description("The function rounds or truncates a timestamp or interval to the date part you need.")]
    [FunctionSignature("date_trunc(field: string, source: timestamp): timestamp")]
    [FunctionSignature("date_trunc(field: string, source: interval): interval")]
    public static VariantValue Trunc(IExecutionThread thread)
    {
        var field = thread.Stack[0].AsString.Trim().ToUpperInvariant();
        var source = thread.Stack[1];
        if (source.IsNull)
        {
            return VariantValue.Null;
        }
        var type = source.Type;
        if (type == DataType.Timestamp)
        {
            var target = source.AsTimestampUnsafe;
            var timestamp = field switch
            {
                "YEAR" or "Y" => new DateTime(target.Year, 1, 1, 0, 0, 0, target.Kind),
                "DAY" or "D" => new DateTime(target.Year, target.Month, target.Day, 0, 0, 0, target.Kind),
                "HOUR" or "H" => new DateTime(target.Year, target.Month, target.Day, target.Hour, 0, 0, target.Kind),
                "MINUTE" or "MIN" or "M" =>
                    new DateTime(target.Year, target.Month, target.Day, target.Hour, target.Minute, 0, target.Kind),
                "SECOND" or "SEC" or "S" =>
                    new DateTime(target.Year, target.Month, target.Day, target.Hour, target.Minute, target.Second, target.Kind),
                _ => throw new SemanticException(string.Format(Resources.Errors.InvalidField, field)),
            };
            return new VariantValue(timestamp);
        }
        else if (type == DataType.Interval)
        {
            var target = source.AsIntervalUnsafe;
            var interval = field switch
            {
                "DAY" or "D" => new TimeSpan(target.Days, 0, 0, 0),
                "HOUR" or "H" => new TimeSpan(target.Days, target.Hours, 0),
                "MINUTE" or "MIN" or "M" => new TimeSpan(target.Days, target.Hours, target.Minutes, 0),
                "SECOND" or "SEC" or "S" =>
                    new TimeSpan(target.Days, target.Hours, target.Minutes, target.Seconds, 0),
                _ => throw new SemanticException(string.Format(Resources.Errors.InvalidField, field)),
            };
            return new VariantValue(interval);
        }
        throw new SemanticException(Resources.Errors.InvalidArgumentType);
    }

    [SafeFunction]
    [Description("The function adds a number to a datepart of an input date, and returns a modified date/time value.")]
    [FunctionSignature("date_add(datepart: string, number: integer, source: timestamp): timestamp")]
    public static VariantValue DateAdd(IExecutionThread thread)
    {
        var datepart = thread.Stack[0].AsString.Trim().ToUpperInvariant();
        var number = (int)(thread.Stack[1].AsInteger ?? 0);
        var source = thread.Stack[2];
        if (source.IsNull || !source.AsTimestamp.HasValue)
        {
            return VariantValue.Null;
        }

        var target = source.AsTimestamp.Value;
        var timestamp = datepart switch
        {
            "YEAR" or "Y" => target.AddYears(number),
            "MONTH" or "MM" => target.AddMonths(number),
            "WEEK" or "W" => target.AddDays(number * 7),
            "DAY" or "D" => target.AddDays(number),
            "HOUR" or "H" => target.AddHours(number),
            "MINUTE" or "MIN" or "M" => target.AddMinutes(number),
            "SECOND" or "SEC" or "S" => target.AddSeconds(number),
            "MS" => target.AddMilliseconds(number),
            _ => throw new SemanticException(string.Format(Resources.Errors.InvalidField, datepart)),
        };
        return new VariantValue(timestamp);
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ToDate);
        functionsManager.RegisterFunction(Now);
        functionsManager.RegisterFunction(Date);
        functionsManager.RegisterFunction(Extract);
        functionsManager.RegisterFunction(Trunc);
        functionsManager.RegisterFunction(DateAdd);
    }
}
