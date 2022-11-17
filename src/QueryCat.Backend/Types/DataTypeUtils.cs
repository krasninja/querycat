using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Types;

/// <summary>
/// The helper class allows to work with types system.
/// </summary>
public static class DataTypeUtils
{
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
        DataType.Object
    };

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
            TypeCode.String => DataType.String,
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

    /// <summary>
    /// Parse interval from string.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <returns>Time span.</returns>
    internal static TimeSpan ParseInterval(string target)
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
            string intervalString = arr[i];

            // Covert short value to full (like 1h -> 1 h).
            if (char.IsLetter(arr[i][^1]))
            {
                intervalType = arr[i][^1].ToString();
                intervalString = arr[i][..^1];
            }
            // Standard case like "1 min".
            else if (i < arr.Length - 1)
            {
                intervalType = arr[++i];
            }
            else
            {
                throw new FormatException("Incorrect number of items for interval.");
            }

            // First must be double.
            if (!double.TryParse(intervalString, out var intervalDouble))
            {
                throw new FormatException("Cannot parse interval as double.");
            }

            var timeSpan = ParseIntervalInternal(intervalDouble, intervalType);
            result += timeSpan;
        }

        return result;
    }

    private static TimeSpan ParseIntervalInternal(double value, string type)
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
        throw new FormatException("Cannot parse interval.");
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

    #region Serialization

    internal static string SerializeVariantValue(VariantValue value)
        => value.GetInternalType() switch
        {
            DataType.Null => "null",
            DataType.Void => "void",
            DataType.Integer => "i:" + value.AsIntegerUnsafe,
            DataType.String => "s:" + StringUtils.Quote(value.AsStringUnsafe).ToString(),
            DataType.Boolean => "bl:" + value.AsBooleanUnsafe,
            DataType.Float => "fl:" + value.AsFloatUnsafe,
            DataType.Timestamp => $"ts:{value.AsTimestampUnsafe.Ticks}:{(int)value.AsTimestampUnsafe.Kind}",
            DataType.Interval => "in:" + value.AsInterval.Ticks,
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
            throw new InvalidOperationException("Invalid deserialization source.");
        }

        var type = source[..colonIndex].ToString();
        var value = source[(colonIndex + 1)..];
        if (type == "i")
        {
            return new VariantValue(int.Parse(value));
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
            return new VariantValue(float.Parse(value));
        }
        if (type == "ts")
        {
            var ticks = long.Parse(value[..^2]);
            var kind = (DateTimeKind)int.Parse(value[^1..]);
            return new VariantValue(new DateTime(ticks, kind));
        }
        if (type == "in")
        {
            var ticks = long.Parse(value);
            return new VariantValue(new TimeSpan(ticks));
        }

        throw new InvalidOperationException($"Invalid deserialization type '{type}'.");
    }

    #endregion
}
