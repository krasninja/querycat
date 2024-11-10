namespace QueryCat.Backend.Core.Types;

public readonly partial struct VariantValue
{
    /// <inheritdoc />
    public TypeCode GetTypeCode() => Type switch
    {
        DataType.Integer => TypeCode.Int64,
        DataType.String => TypeCode.String,
        DataType.Float => TypeCode.Double,
        DataType.Void => TypeCode.DBNull,
        DataType.Null => TypeCode.DBNull,
        DataType.Object => TypeCode.Object,
        DataType.Dynamic => TypeCode.Object,
        DataType.Boolean => TypeCode.Boolean,
        DataType.Timestamp => TypeCode.DateTime,
        DataType.Numeric => TypeCode.Decimal,
        DataType.Blob => TypeCode.Object,
        _ => TypeCode.Empty,
    };

    /// <inheritdoc />
    public bool ToBoolean(IFormatProvider? provider = null)
    {
        var result = AsBooleanNullable;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return result.Value;
    }

    /// <inheritdoc />
    public byte ToByte(IFormatProvider? provider = null)
    {
        var result = AsInteger;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return (byte)result.Value;
    }

    /// <inheritdoc />
    public char ToChar(IFormatProvider? provider = null)
    {
        var result = AsString;
        if (result.Length != 1)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return result[0];
    }

    /// <inheritdoc />
    public DateTime ToDateTime(IFormatProvider? provider = null)
    {
        var result = AsTimestamp;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return result.Value;
    }

    /// <inheritdoc />
    public decimal ToDecimal(IFormatProvider? provider = null)
    {
        var result = AsNumeric;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return result.Value;
    }

    /// <inheritdoc />
    public double ToDouble(IFormatProvider? provider = null)
    {
        var result = AsFloat;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return result.Value;
    }

    /// <inheritdoc />
    public short ToInt16(IFormatProvider? provider = null)
    {
        var result = AsInteger;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return (short)result.Value;
    }

    /// <inheritdoc />
    public int ToInt32(IFormatProvider? provider = null)
    {
        var result = AsInteger;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return (int)result.Value;
    }

    /// <inheritdoc />
    public long ToInt64(IFormatProvider? provider = null)
    {
        var result = AsInteger;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return result.Value;
    }

    /// <inheritdoc />
    public sbyte ToSByte(IFormatProvider? provider = null)
    {
        var result = AsInteger;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return (sbyte)result.Value;
    }

    /// <inheritdoc />
    public float ToSingle(IFormatProvider? provider = null)
    {
        var result = AsFloat;
        if (!result.HasValue)
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return (float)result.Value;
    }

    /// <inheritdoc />
    public ushort ToUInt16(IFormatProvider? provider = null)
    {
        var result = AsInteger;
        if (!result.HasValue)
        {
            throw new FormatException(
                string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return (ushort)result.Value;
    }

    /// <inheritdoc />
    public uint ToUInt32(IFormatProvider? provider = null)
    {
        var result = AsInteger;
        if (!result.HasValue)
        {
            throw new FormatException(
                string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return (uint)result.Value;
    }

    /// <inheritdoc />
    public ulong ToUInt64(IFormatProvider? provider = null)
    {
        var result = AsInteger;
        if (!result.HasValue)
        {
            throw new FormatException(
                string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return (ulong)result.Value;
    }

    /// <inheritdoc />
    public object ToType(Type conversionType, IFormatProvider? provider = null)
    {
        if (Type != DataType.Object)
        {
            throw new FormatException(
                string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        var result = AsObjectUnsafe;
        if (result == null || !conversionType.IsInstanceOfType(result))
        {
            throw new FormatException(string.Format(Resources.Errors.InvalidValueFormat, AsString));
        }
        return result;
    }
}
