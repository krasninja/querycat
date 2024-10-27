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
        DataType.Numeric => typeof(decimal),
        DataType.Blob => typeof(byte[]),
        DataType.Object => typeof(object),
        DataType.Dynamic => typeof(object),
        DataType.Null => typeof(void),
        DataType.Void => typeof(void),
        _ => typeof(void)
    };

    /// <summary>
    /// Convert .NET BCL system type into QueryCat type.
    /// </summary>
    /// <param name="type">System type.</param>
    /// <returns>Application type.</returns>
    public static DataType ConvertFromSystem(Type type)
    {
        if (type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GetGenericArguments()[0];
        }

        if (type.IsEnum)
        {
            return DataType.String;
        }
        if (typeof(DateTime).IsAssignableFrom(type))
        {
            return DataType.Timestamp;
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
        if (typeof(byte[]).IsAssignableFrom(type) || typeof(IBlobData).IsAssignableFrom(type))
        {
            return DataType.Blob;
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
        var type = value.Type;
        return type switch
        {
            DataType.Integer => (TEnum)Enum.ToObject(typeof(TEnum), value.AsIntegerUnsafe),
            DataType.String => Enum.Parse<TEnum>(value.AsString, ignoreCase: true),
            _ => throw new ArgumentException(Resources.Errors.InvalidValueType),
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
            DataType.Numeric => value.AsNumeric,
            DataType.Object => ConvertToObject(value),
            DataType.Dynamic => ConvertToObject(value),
            DataType.String => value.AsString,
            DataType.Timestamp => value.AsTimestamp,
            DataType.Blob => value.AsBlobUnsafe.GetStream(),
            DataType.Null => null,
            DataType.Void => null,
            _ => throw new InvalidOperationException(
                string.Format(Resources.Errors.CannotConvertToType, value.Type, targetType)),
        };

        if (result != null && result.GetType() != targetType && result is IConvertible convertible)
        {
            return convertible.ToType(targetType, Application.Culture);
        }

        return result;
    }

    private static object? ConvertToObject(VariantValue value)
        => value.Type switch
        {
            DataType.Boolean => value.AsBooleanUnsafe,
            DataType.Float => value.AsFloatUnsafe,
            DataType.Integer => value.AsIntegerUnsafe,
            DataType.Interval => value.AsIntervalUnsafe,
            DataType.Numeric => value.AsNumericUnsafe,
            DataType.Object => value.AsObjectUnsafe,
            DataType.Dynamic => value.AsObjectUnsafe,
            DataType.String => value.AsStringUnsafe,
            DataType.Timestamp => value.AsTimestampUnsafe,
            DataType.Blob => value.AsBlobUnsafe.GetStream(),
            DataType.Void => null,
            DataType.Null => null,
            _ => null,
        };
}
