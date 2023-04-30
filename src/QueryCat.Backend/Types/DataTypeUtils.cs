using System.Globalization;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Types;

/// <summary>
/// The helper class allows to work with types system.
/// </summary>
public static class DataTypeUtils
{
    private static readonly ILogger Logger = Application.LoggerFactory.CreateLogger(typeof(DataTypeUtils));

    /// <summary>
    /// Contains the types that can be used for row column.
    /// </summary>
    internal static DataType[] RowDataTypes => new[]
    {
        DataType.Integer,
        DataType.String,
        DataType.Float,
        DataType.Integer,
        DataType.Timestamp,
        DataType.Interval,
        DataType.Boolean,
        DataType.Numeric,
        DataType.Object,
        DataType.Void,
    };

    /// <summary>
    /// Is this is a simple type (not an object).
    /// </summary>
    /// <param name="dataType">Data type.</param>
    /// <returns><c>True</c> if type is simple, <c>false</c> otherwise.</returns>
    public static bool IsSimple(DataType dataType)
        =>
            IsNumeric(dataType) || dataType == DataType.Boolean || dataType == DataType.String
                || dataType == DataType.Timestamp || dataType == DataType.Interval;

    /// <summary>
    /// Is the data type numeric.
    /// </summary>
    /// <param name="dataType">Data type to check.</param>
    /// <returns><c>True</c> if it is numeric, <c>false</c> otherwise.</returns>
    public static bool IsNumeric(DataType dataType)
        => dataType == DataType.Integer || dataType == DataType.Float || dataType == DataType.Numeric;

    public static bool EqualsWithCast(DataType dataType1, DataType dataType2)
    {
        if (IsNumeric(dataType1) && IsNumeric(dataType2))
        {
            return true;
        }
        return dataType1 == dataType2;
    }

    #region Convert

    /// <summary>
    /// Convert QueryCat type into .NET BCL system type.
    /// </summary>
    /// <param name="dataType">Application type.</param>
    /// <returns>.NET type.</returns>
    public static Type ConvertToSystem(DataType dataType) => dataType switch
    {
        DataType.Boolean => typeof(bool),
        DataType.Float => typeof(double),
        DataType.Integer => typeof(long),
        DataType.String => typeof(string),
        DataType.Timestamp => typeof(DateTime),
        DataType.Interval => typeof(TimeSpan),
        DataType.Null => typeof(void),
        DataType.Void => typeof(void),
        DataType.Numeric => typeof(decimal),
        DataType.Object => typeof(object),
        _ => typeof(void)
    };

    /// <summary>
    /// Convert .NET BCL system type into QueryCat type.
    /// </summary>
    /// <param name="type">System type.</param>
    /// <returns>Application type.</returns>
    public static DataType ConvertFromSystem(Type type)
    {
        if (type.IsEnum)
        {
            return DataType.String;
        }

        if (typeof(DateTimeOffset).IsAssignableFrom(type))
        {
            return DataType.Timestamp;
        }
        if (typeof(TimeSpan).IsAssignableFrom(type))
        {
            return DataType.Interval;
        }
        if (type == typeof(string))
        {
            return DataType.String;
        }
        if (!type.IsValueType)
        {
            return DataType.Object;
        }

        return GetTypeCode(type) switch
        {
            TypeCode.Byte => DataType.Integer,
            TypeCode.SByte => DataType.Integer,
            TypeCode.UInt16 => DataType.Integer,
            TypeCode.UInt32 => DataType.Integer,
            TypeCode.UInt64 => DataType.Integer,
            TypeCode.Int16 => DataType.Integer,
            TypeCode.Int32 => DataType.Integer,
            TypeCode.Int64 => DataType.Integer,
            TypeCode.Double => DataType.Float,
            TypeCode.Single => DataType.Float,
            TypeCode.Boolean => DataType.Boolean,
            TypeCode.Decimal => DataType.Numeric,
            TypeCode.DateTime => DataType.Timestamp,
            _ => DataType.Void
        };
    }

    private static TypeCode GetTypeCode(Type type)
    {
        return type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
            ? Type.GetTypeCode(Nullable.GetUnderlyingType(type))
            : Type.GetTypeCode(type);
    }

    /// <summary>
    /// Convert variant value to enum.
    /// </summary>
    /// <param name="value">Variant value.</param>
    /// <typeparam name="TEnum">Enum type.</typeparam>
    /// <returns>Enum value.</returns>
    public static TEnum ConvertToEnum<TEnum>(in VariantValue value) where TEnum : struct
    {
        var type = value.GetInternalType();
        return type switch
        {
            DataType.Integer => (TEnum)Enum.ToObject(typeof(TEnum), value.AsInteger),
            DataType.String => Enum.Parse<TEnum>(value.AsString, ignoreCase: true),
            _ => throw new ArgumentException("Invalid value type."),
        };
    }

    /// <summary>
    /// Convert value to the related system type.
    /// </summary>
    /// <param name="value">Variant value.</param>
    /// <param name="targetType">Target type.</param>
    /// <returns>Converted object.</returns>
    public static object? ConvertValue(VariantValue value, Type targetType)
    {
        if (value.IsNull)
        {
            return null;
        }

        var relatedType = ConvertFromSystem(targetType);
        var result = relatedType switch
        {
            DataType.Boolean => value.AsBoolean,
            DataType.Float => value.AsFloat,
            DataType.Integer => value.AsInteger,
            DataType.Interval => value.AsInterval,
            DataType.Null => null,
            DataType.Numeric => value.AsNumeric,
            DataType.Object => value.AsObject,
            DataType.String => value.AsString,
            DataType.Timestamp => value.AsTimestamp,
            _ => throw new InvalidOperationException(
                $"Cannot convert value from system type '{targetType}' to type '{relatedType}'."),
        };

        if (result != null && result.GetType() != targetType && result is IConvertible convertible)
        {
            return convertible.ToType(targetType, CultureInfo.InvariantCulture);
        }

        return result;
    }

    #endregion

    #region Timestamp / Interval

    /// <summary>
    /// Parse interval from string.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <param name="result">Parsed interval result.</param>
    /// <returns><c>True</c> if parsed successfully, <c>false</c> otherwise.</returns>
    internal static bool TryParseInterval(string target, out TimeSpan result)
    {
        var interval = ParseIntervalInternal(target, throwExceptions: false);
        if (!interval.HasValue)
        {
            result = default;
            return false;
        }
        result = interval.Value;
        return true;
    }

    /// <summary>
    /// Parse interval from string.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <returns>Time span.</returns>
    internal static TimeSpan ParseInterval(string target)
        => ParseIntervalInternal(target, throwExceptions: true)!.Value;

    private static TimeSpan? ParseIntervalInternal(string target, bool throwExceptions = true)
    {
        if (TimeSpan.TryParse(target, out var resultTimeSpan))
        {
            return resultTimeSpan;
        }

        var result = TimeSpan.Zero;
        var arr = target.ToUpper().Split(' ',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < arr.Length; i++)
        {
            string intervalType;
            var intervalString = arr[i];

            // Put a space between number and part (like 1h -> 1 h, 2sec -> 2 sec).
            var firstLetterIndex = GetFirstLetterIndex(intervalString);
            if (firstLetterIndex > -1)
            {
                intervalType = arr[i].Substring(firstLetterIndex);
                intervalString = arr[i].Substring(0, firstLetterIndex).Trim();
            }
            // Standard case like "1 min".
            else if (i < arr.Length - 1)
            {
                intervalType = arr[++i];
            }
            else
            {
                if (throwExceptions)
                {
                    throw new FormatException("Incorrect number of items for interval.");
                }
                return null;
            }

            // First must be double.
            if (!double.TryParse(intervalString, out var intervalDouble))
            {
                if (throwExceptions)
                {
                    throw new FormatException("Cannot parse interval as double.");
                }
                return null;
            }

            var timeSpan = ParseIntervalType(intervalDouble, intervalType);
            if (!timeSpan.HasValue)
            {
                return null;
            }
            result += timeSpan.Value;
        }

        return result;
    }

    private static int GetFirstLetterIndex(string str)
    {
        for (var i = 0; i < str.Length; i++)
        {
            if (char.IsLetter(str[i]))
            {
                return i;
            }
        }
        return -1;
    }

    private static TimeSpan? ParseIntervalType(double value, string type, bool throwExceptions = true)
    {
        switch (type)
        {
            case "MS":
            case "MILLISECOND":
            case "MILLISECONDS":
                return TimeSpan.FromMilliseconds(value);
            case "S":
            case "SEC":
            case "SECOND":
            case "SECONDS":
                return TimeSpan.FromSeconds(value);
            case "M":
            case "MIN":
            case "MINUTE":
            case "MINUTES":
                return TimeSpan.FromMinutes(value);
            case "H":
            case "HOUR":
            case "HOURS":
                return TimeSpan.FromHours(value);
            case "D":
            case "DAY":
            case "DAYS":
                return TimeSpan.FromDays(value);
        }
        if (throwExceptions)
        {
            throw new FormatException("Cannot parse interval.");
        }
        return null;
    }

    #endregion

    #region Serialization

    internal static string SerializeVariantValue(VariantValue value)
        => value.GetInternalType() switch
        {
            DataType.Null => "null",
            DataType.Void => "void",
            DataType.Integer => "i:" + value.AsIntegerUnsafe.ToString(CultureInfo.InvariantCulture),
            DataType.String => "s:" + StringUtils.Quote(value.AsStringUnsafe).ToString(),
            DataType.Boolean => "bl:" + value.AsBooleanUnsafe.ToString(CultureInfo.InvariantCulture),
            DataType.Float => "fl:" + value.AsFloatUnsafe.ToString("G17", CultureInfo.InvariantCulture),
            DataType.Numeric => "n:" + value.AsNumericUnsafe.ToString(CultureInfo.InvariantCulture),
            DataType.Timestamp => $"ts:{value.AsTimestampUnsafe.Ticks}:{(int)value.AsTimestampUnsafe.Kind}",
            DataType.Interval => "in:" + value.AsInterval.Ticks.ToString(CultureInfo.InvariantCulture),
            _ => string.Empty
        };

    internal static VariantValue DeserializeVariantValue(ReadOnlySpan<char> source)
    {
        if (source == "null" || source == "void")
        {
            return VariantValue.Null;
        }
        var colonIndex = source.IndexOf(':');
        if (colonIndex == -1)
        {
            Logger.LogWarning("Invalid deserialization source.");
            return VariantValue.Null;
        }

        var type = source[..colonIndex].ToString();
        var value = source[(colonIndex + 1)..];
        if (type == "i")
        {
            return new VariantValue(int.Parse(value, CultureInfo.InvariantCulture));
        }
        if (type == "s")
        {
            return new VariantValue(StringUtils.Unquote(value));
        }
        if (type == "bl")
        {
            return new VariantValue(bool.Parse(value));
        }
        if (type == "fl")
        {
            return new VariantValue(float.Parse(value, CultureInfo.InvariantCulture));
        }
        if (type == "n")
        {
            return new VariantValue(decimal.Parse(value, CultureInfo.InvariantCulture));
        }
        if (type == "ts")
        {
            var ticks = long.Parse(value[..^2], CultureInfo.InvariantCulture);
            var kind = (DateTimeKind)int.Parse(value[^1..], CultureInfo.InvariantCulture);
            return new VariantValue(new DateTime(ticks, kind));
        }
        if (type == "in")
        {
            var ticks = long.Parse(value, CultureInfo.InvariantCulture);
            return new VariantValue(new TimeSpan(ticks));
        }

        throw new InvalidOperationException($"Invalid deserialization type '{type}'.");
    }

    #endregion
}
