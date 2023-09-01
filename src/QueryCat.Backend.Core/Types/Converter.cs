using System.Globalization;

namespace QueryCat.Backend.Core.Types;

/// <summary>
/// Convert to/from QueryCat types and .NET types.
/// </summary>
public static class Converter
{
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

        if (type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GetGenericArguments()[0];
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
}
