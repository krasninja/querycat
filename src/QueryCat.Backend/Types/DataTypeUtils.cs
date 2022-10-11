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

    public static bool IsSimple(DataType dataType)
        =>
            IsNumeric(dataType) || dataType == DataType.Boolean || dataType == DataType.String
            || dataType == DataType.Timestamp;

    /// <summary>
    /// Is the data type numeric.
    /// </summary>
    /// <param name="dataType">Data type to check.</param>
    /// <returns><c>True</c> if it is numeric, <c>false</c> otherwise.</returns>
    public static bool IsNumeric(DataType dataType)
        => dataType == DataType.Integer || dataType == DataType.Float || dataType == DataType.Numeric;

    public static bool EqualsWithCase(DataType dataType1, DataType dataType2)
    {
        if (IsNumeric(dataType1) && IsNumeric(dataType2))
        {
            return true;
        }
        return dataType1 == dataType2;
    }
}
