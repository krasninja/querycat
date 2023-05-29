using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace QueryCat.Backend.Types;

/// <summary>
/// The union-like class. It holds only one value of the specified data type.
/// In fact it has two 64-bit fields.
/// </summary>
public readonly partial struct VariantValue : IEquatable<VariantValue>
{
    public const string TrueValueString = "TRUE";
    public const string FalseValueString = "FALSE";

    private static readonly DataTypeObject IntegerObject = new("INT");
    private static readonly DataTypeObject FloatObject = new("FLOAT");
    private static readonly DataTypeObject TimestampObject = new("TIMESTAMP");
    private static readonly DataTypeObject IntervalObject = new("INTERVAL");
    private static readonly DataTypeObject BooleanObject = new("BOOL");

    public static VariantValue OneIntegerValue = new(1);
    public static VariantValue TrueValue = new(true);
    public static VariantValue FalseValue = new(false);

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct TypeUnion
    {
        [FieldOffset(0)]
        internal readonly long IntegerValue;

        [FieldOffset(0)]
        internal readonly double DoubleValue;

        [FieldOffset(0)]
        internal readonly DateTime DateTimeValue;

        [FieldOffset(0)]
        internal readonly bool BooleanValue;

        [FieldOffset(0)]
        internal readonly TimeSpan TimeSpanValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TypeUnion(long value) : this()
        {
            IntegerValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TypeUnion(double value) : this()
        {
            DoubleValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TypeUnion(DateTime value) : this()
        {
            DateTimeValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TypeUnion(bool value) : this()
        {
            BooleanValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TypeUnion(TimeSpan value) : this()
        {
            TimeSpanValue = value;
        }
    }

    private readonly TypeUnion _valueUnion;

    private readonly object? _object = null;

    /// <summary>
    /// NULL value.
    /// </summary>
    public static VariantValue Null = default;

    #region Constructors

    private VariantValue(in TypeUnion union, object? obj)
    {
        _valueUnion = union;
        _object = obj;
    }

    public VariantValue(DataType type)
    {
        _object = type switch
        {
            DataType.Integer => IntegerObject,
            DataType.String => string.Empty,
            DataType.Boolean => BooleanObject,
            DataType.Float => FloatObject,
            DataType.Timestamp => TimestampObject,
            DataType.Interval => IntervalObject,
            DataType.Object => null,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
        _valueUnion = default;
    }

    public VariantValue(long value)
    {
        _valueUnion = new TypeUnion(value);
        _object = IntegerObject;
    }

    public VariantValue(int value)
    {
        _valueUnion = new TypeUnion(value);
        _object = IntegerObject;
    }

    public VariantValue(string? value)
    {
        _object = value;
        _valueUnion = default;
    }

    public VariantValue(ReadOnlySpan<char> value)
    {
        _object = value.ToString();
        _valueUnion = default;
    }

    public VariantValue(double value)
    {
        _valueUnion = new TypeUnion(value);
        _object = FloatObject;
    }

    public VariantValue(DateTime value)
    {
        _valueUnion = new TypeUnion(value);
        _object = TimestampObject;
    }

    public VariantValue(TimeSpan value)
    {
        _valueUnion = new TypeUnion(value);
        _object = IntervalObject;
    }

    public VariantValue(bool value)
    {
        _valueUnion = new TypeUnion(value);
        _object = BooleanObject;
    }

    public VariantValue(decimal value)
    {
        _object = value;
        _valueUnion = default;
    }

    private VariantValue(object obj)
    {
        _object = obj;
        _valueUnion = default;
    }

    public static VariantValue CreateFromObject<T>(in T obj)
    {
        if (obj == null)
        {
            return Null;
        }

        if (obj is VariantValue variantValue)
        {
            return variantValue;
        }
        if (obj is Int64
            || obj is Int32
            || obj is Int16
            || obj is Byte
            || obj is UInt64
            || obj is UInt32
            || obj is UInt16
            || obj is SByte)
        {
            return new VariantValue(Convert.ToInt64(obj));
        }
        if (obj is bool objBool)
        {
            return new VariantValue(objBool);
        }
        if (obj is double || obj is float)
        {
            return new VariantValue(Convert.ToDouble(obj));
        }
        if (obj is decimal objDecimal)
        {
            return new VariantValue(objDecimal);
        }
        if (obj is string || typeof(T).IsEnum)
        {
            return new VariantValue(Convert.ToString(obj));
        }
        if (obj is DateTime || obj is DateOnly)
        {
            return new VariantValue(Convert.ToDateTime(obj));
        }
        if (obj is DateTimeOffset dateTimeOffset)
        {
            return new VariantValue(dateTimeOffset.UtcDateTime);
        }
        if (obj is TimeSpan timeSpan)
        {
            return new VariantValue(timeSpan);
        }
        if (obj is JsonValue jsonValue)
        {
            var jsonType = jsonValue.GetValue<JsonElement>().ValueKind;
            if (jsonType == JsonValueKind.Number)
            {
                if (jsonValue.TryGetValue(out long jsonLongValue))
                {
                    return new VariantValue(jsonLongValue);
                }
                if (jsonValue.TryGetValue(out decimal jsonDecimalValue))
                {
                    return new VariantValue(jsonDecimalValue);
                }
                if (jsonValue.TryGetValue(out double jsonDoubleValue))
                {
                    return new VariantValue(jsonDoubleValue);
                }
            }
            if (jsonType == JsonValueKind.String && jsonValue.TryGetValue(out string? jsonStringValue))
            {
                return new VariantValue(jsonStringValue);
            }
            if (jsonType == JsonValueKind.True)
            {
                return TrueValue;
            }
            if (jsonType == JsonValueKind.False)
            {
                return FalseValue;
            }
            if (jsonType == JsonValueKind.Null || jsonType == JsonValueKind.Undefined)
            {
                return Null;
            }
        }
        return new VariantValue(obj);
    }

    /// <summary>
    /// Get internal variant value type.
    /// </summary>
    /// <returns>Data type.</returns>
    public DataType GetInternalType()
    {
        if (_object == null)
        {
            return DataType.Null;
        }
        if (_object == IntegerObject)
        {
            return DataType.Integer;
        }
        if (_object is string)
        {
            return DataType.String;
        }
        if (_object == FloatObject)
        {
            return DataType.Float;
        }
        if (_object == BooleanObject)
        {
            return DataType.Boolean;
        }
        if (_object == TimestampObject)
        {
            return DataType.Timestamp;
        }
        if (_object == IntervalObject)
        {
            return DataType.Interval;
        }
        if (_object is decimal)
        {
            return DataType.Numeric;
        }
        if (_object != null)
        {
            return DataType.Object;
        }
        throw new InvalidOperationException("Cannot get type.");
    }

    private bool IsValueType() =>
        _object == IntegerObject || _object == FloatObject ||
        _object == BooleanObject || _object == TimestampObject ||
        _object == IntervalObject;

    #endregion

    #region Getters and Setters

    public long AsInteger => CheckTypeAndTryToCast(DataType.Integer)._valueUnion.IntegerValue;

    internal long AsIntegerUnsafe => _valueUnion.IntegerValue;

    public string AsString
    {
        get
        {
            var obj = CheckTypeAndTryToCast(DataType.String)._object;
            return obj != null ? (string)obj : string.Empty;
        }
    }

    internal string AsStringUnsafe => _object != null ? (string)_object : string.Empty;

    public double AsFloat => CheckTypeAndTryToCast(DataType.Float)._valueUnion.DoubleValue;

    internal double AsFloatUnsafe => _valueUnion.DoubleValue;

    public DateTime AsTimestamp => CheckTypeAndTryToCast(DataType.Timestamp)._valueUnion.DateTimeValue;

    internal DateTime AsTimestampUnsafe => _valueUnion.DateTimeValue;

    public TimeSpan AsInterval => CheckTypeAndTryToCast(DataType.Interval)._valueUnion.TimeSpanValue;

    internal TimeSpan AsIntervalUnsafe => _valueUnion.TimeSpanValue;

    public bool AsBoolean => CheckTypeAndTryToCast(DataType.Boolean)._valueUnion.BooleanValue;

    internal bool AsBooleanUnsafe => _valueUnion.BooleanValue;

    public bool? AsBooleanNullable => !IsNull
        ? CheckTypeAndTryToCast(DataType.Boolean)._valueUnion.BooleanValue
        : null;

    public decimal AsNumeric => (decimal)CheckTypeAndTryToCast(DataType.Numeric)._object!;

    internal decimal AsNumericUnsafe => (decimal)_object!;

    public object? AsObject => CheckTypeAndTryToCast(DataType.Object)._object;

    internal object? AsObjectUnsafe => _object;

    #endregion

    /// <summary>
    /// Get object of specified type.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    /// <returns>Object.</returns>
    public T GetAsObject<T>()
    {
        var sourceObj = CheckTypeAndTryToCast(DataType.Object)._object;
        if (sourceObj == null)
        {
            throw new InvalidOperationException("Object is null.");
        }
        if (sourceObj is T obj)
        {
            return obj;
        }
        throw new InvalidOperationException(
            $"Cannot cast object of type '{sourceObj.GetType()}' to type '{typeof(T)}'.");
    }

    /// <summary>
    /// Get the internal value as .NET object.
    /// </summary>
    /// <returns>Value object.</returns>
    public object? GetGenericObject() => GetInternalType() switch
    {
        DataType.Null => null,
        DataType.Void => null,
        DataType.Integer => AsIntegerUnsafe,
        DataType.String => AsStringUnsafe,
        DataType.Boolean => AsBooleanUnsafe,
        DataType.Float => AsFloatUnsafe,
        DataType.Numeric => AsNumericUnsafe,
        DataType.Timestamp => AsTimestampUnsafe,
        DataType.Interval => AsIntervalUnsafe,
        DataType.Object => AsObjectUnsafe,
        _ => null,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValue CheckTypeAndTryToCast(DataType targetType)
    {
        var currentType = GetInternalType();
        if (targetType != currentType)
        {
            return Cast(targetType);
        }
        return this;
    }

    /// <summary>
    /// Determines whether the value is null.
    /// </summary>
    public bool IsNull => _object == null;

    #region Casting

    private static VariantValue StringToInteger(string? value, out bool success)
        => StringToInteger((ReadOnlySpan<char>)value, out success);

    private static VariantValue StringToInteger(in ReadOnlySpan<char> value, out bool success)
    {
        success = int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var @out);
        return success ? new VariantValue(@out) : Null;
    }

    private static VariantValue StringToTimestamp(string? value, out bool success)
        => StringToTimestamp((ReadOnlySpan<char>)value, out success);

    private static readonly string[] DateTimeAdditionalFormats =
    {
        "yyMMdd",
        "yyyyMMdd"
    };

    private static VariantValue StringToTimestamp(in ReadOnlySpan<char> value, out bool success)
    {
        success = DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var @out);
        if (!success)
        {
            success = DateTime.TryParseExact(value, DateTimeAdditionalFormats, null,
                DateTimeStyles.AllowWhiteSpaces, out @out);
        }
        return success ? new VariantValue(@out) : Null;
    }

    private static VariantValue StringToInterval(in ReadOnlySpan<char> value, out bool success)
    {
        success = IntervalParser.TryParseInterval(value.ToString(), out var @out);
        return success ? new VariantValue(@out) : Null;
    }

    private static VariantValue StringToFloat(string? value, out bool success)
        => StringToFloat((ReadOnlySpan<char>)value, out success);

    private static VariantValue StringToFloat(in ReadOnlySpan<char> value, out bool success)
    {
        success = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var @out);
        return success ? new VariantValue(@out) : Null;
    }

    private static VariantValue StringToBoolean(string? value, out bool success)
        => StringToBoolean((ReadOnlySpan<char>)value, out success);

    private static VariantValue StringToBoolean(in ReadOnlySpan<char> value, out bool success)
    {
        if (value.IsEmpty)
        {
            success = false;
            return Null;
        }

        if (value.Equals(TrueValueString, StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("YES", StringComparison.OrdinalIgnoreCase)
            || value.Equals("ON", StringComparison.OrdinalIgnoreCase)
            || value.Equals("T", StringComparison.OrdinalIgnoreCase))
        {
            success = true;
            return new VariantValue(true);
        }

        if (value.Equals(FalseValueString, StringComparison.OrdinalIgnoreCase)
            || value.Equals("0", StringComparison.OrdinalIgnoreCase)
            || value.Equals("NO", StringComparison.OrdinalIgnoreCase)
            || value.Equals("OFF", StringComparison.OrdinalIgnoreCase)
            || value.Equals("F", StringComparison.OrdinalIgnoreCase))
        {
            success = true;
            return new VariantValue(false);
        }

        success = false;
        return Null;
    }

    private static VariantValue StringToNumeric(string? value, out bool success)
        => StringToNumeric((ReadOnlySpan<char>)value, out success);

    private static VariantValue StringToNumeric(in ReadOnlySpan<char> value, out bool success)
    {
        success = decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var @out);
        return success ? new VariantValue(@out) : Null;
    }

    private static VariantValue StringToString(string? value, out bool success)
    {
        success = true;
        return new VariantValue(value);
    }

    private static VariantValue StringToString(in ReadOnlySpan<char> value, out bool success)
    {
        success = true;
        return new VariantValue(value);
    }

    /// <summary>
    /// Attempt to convert type of the variant value to another one.
    /// </summary>
    /// <param name="targetType">Target type to convert.</param>
    /// <param name="output">Return value.</param>
    /// <returns><c>True</c> if cast was successful, <c>false</c> otherwise.</returns>
    public bool TryCast(in DataType targetType, out VariantValue output)
    {
        // Null value is always null.
        if (IsNull)
        {
            output = Null;
            return true;
        }

        var sourceType = GetInternalType();
        if (sourceType == targetType)
        {
            output = this;
            return true;
        }

        var success = true;
        output = sourceType switch
        {
            DataType.Integer => targetType switch
            {
                DataType.String => new(_valueUnion.IntegerValue.ToString()),
                DataType.Float => new((double)_valueUnion.IntegerValue),
                DataType.Boolean => new(_valueUnion.IntegerValue != 0),
                DataType.Numeric => new((decimal)_valueUnion.IntegerValue),
                _ => Null
            },
            DataType.String => targetType switch
            {
                DataType.Integer => StringToInteger(_object as string, out success),
                DataType.Float => StringToFloat(_object as string, out success),
                DataType.Timestamp => StringToTimestamp(_object as string, out success),
                DataType.Boolean => StringToBoolean(_object as string, out success),
                DataType.Numeric => StringToNumeric(_object as string, out success),
                DataType.Interval => StringToInterval(_object as string, out success),
                _ => Null
            },
            DataType.Float => targetType switch
            {
                DataType.Integer => new((int)_valueUnion.DoubleValue),
                DataType.String => new(_valueUnion.DoubleValue.ToString(CultureInfo.InvariantCulture)),
                DataType.Numeric => new((decimal)_valueUnion.DoubleValue),
                _ => Null
            },
            DataType.Timestamp => targetType switch
            {
                DataType.String => new(_valueUnion.DateTimeValue.ToString(CultureInfo.InvariantCulture)),
                _ => Null
            },
            DataType.Boolean => targetType switch
            {
                DataType.Integer => new(_valueUnion.BooleanValue ? 1 : 0),
                DataType.String => new(_valueUnion.BooleanValue ? TrueValueString : FalseValueString),
                _ => Null
            },
            DataType.Numeric => targetType switch
            {
                DataType.Integer => new(decimal.ToInt64((decimal)_object!)),
                DataType.String => new(((decimal)_object!).ToString(CultureInfo.InvariantCulture)),
                DataType.Float => new(decimal.ToDouble((decimal)_object!)),
                _ => Null
            },
            _ => Null
        };
        return !output.IsNull && success;
    }

    /// <summary>
    /// Convert to target type.
    /// </summary>
    /// <param name="targetType">Target data type.</param>
    /// <returns>Result value.</returns>
    public VariantValue Cast(in DataType targetType)
    {
        if (TryCast(targetType, out var result))
        {
            return result;
        }
        else
        {
            var sourceType = GetInternalType();
            throw new InvalidVariantTypeException(sourceType, targetType);
        }
    }

    /// <summary>
    /// Try create variant value from string.
    /// </summary>
    /// <param name="value">String value.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="variantValue">Out variant value.</param>
    /// <returns><c>True</c> if created successfully, <c>false</c> otherwise.</returns>
    public static bool TryCreateFromString(
        string value,
        in DataType targetType,
        out VariantValue variantValue) => TryCreateFromString((ReadOnlySpan<char>)value, targetType, out variantValue);

    /// <summary>
    /// Try create variant value from string.
    /// </summary>
    /// <param name="value">String value.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="variantValue">Out variant value.</param>
    /// <returns><c>True</c> if created successfully, <c>false</c> otherwise.</returns>
    public static bool TryCreateFromString(
        in ReadOnlySpan<char> value,
        in DataType targetType,
        out VariantValue variantValue)
    {
        var success = false;
        variantValue = targetType switch
        {
            DataType.Integer => StringToInteger(value, out success),
            DataType.Float => StringToFloat(value, out success),
            DataType.Timestamp => StringToTimestamp(value, out success),
            DataType.Boolean => StringToBoolean(value, out success),
            DataType.Numeric => StringToNumeric(value, out success),
            DataType.String => StringToString(value, out success),
            DataType.Interval => StringToInterval(value, out success),
            _ => Null
        };
        return success;
    }

    #endregion

    public static bool Equals(in VariantValue value1, in VariantValue value2) =>
        value1._valueUnion.IntegerValue == value2._valueUnion.IntegerValue
            && ((value1._object == null && value2._object == null) || Equals(value1._object, value2._object));

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is VariantValue vv
            && Equals(this, in vv);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (_object == null)
        {
            return 0;
        }
        return IsValueType() ? _valueUnion.GetHashCode() : _object.GetHashCode();
    }

    public static bool operator ==(VariantValue left, VariantValue right) => left.Equals(right);

    public static bool operator !=(VariantValue left, VariantValue right) => !(left == right);

    public static VariantValue operator +(VariantValue left, VariantValue right)
        => Add(in left, in right, out _);

    public static VariantValue operator -(VariantValue left, VariantValue right)
        => Subtract(in left, in right, out _);

    public static VariantValue operator *(VariantValue left, VariantValue right)
        => Mul(in left, in right, out _);

    public static VariantValue operator /(VariantValue left, VariantValue right)
        => Div(in left, in right, out _);

    public static implicit operator decimal(VariantValue value) => value.AsNumeric;

    public static implicit operator int(VariantValue value) => (int)value.AsInteger;

    public static implicit operator long(VariantValue value) => value.AsInteger;

    public static implicit operator bool(VariantValue value) => value.AsBoolean;

    public static implicit operator double(VariantValue value) => value.AsFloat;

    public static implicit operator string(VariantValue value) => value.AsString;

    public static implicit operator DateTime(VariantValue value) => value.AsTimestamp;

    public static implicit operator TimeSpan(VariantValue value) => value.AsInterval;

    /// <inheritdoc />
    public override string ToString() => GetInternalType() switch
    {
        DataType.Null => "NULL",
        DataType.Void => "VOID",
        DataType.Integer => AsIntegerUnsafe.ToString(CultureInfo.InvariantCulture),
        DataType.String => AsStringUnsafe,
        DataType.Boolean => AsBooleanUnsafe.ToString(CultureInfo.InvariantCulture),
        DataType.Float => AsFloatUnsafe.ToString("F2", CultureInfo.InvariantCulture),
        DataType.Numeric => AsNumericUnsafe.ToString("F", CultureInfo.InvariantCulture),
        DataType.Timestamp => AsTimestampUnsafe.ToString(CultureInfo.InvariantCulture),
        DataType.Interval => AsIntervalUnsafe.ToString("c", CultureInfo.InvariantCulture),
        DataType.Object => "object: " + AsObjectUnsafe,
        _ => "unknown"
    };

    /// <summary>
    /// Convert to string according to the format.
    /// </summary>
    /// <param name="format">Format string.</param>
    /// <returns>String representation.</returns>
    public string ToString(string format) => GetInternalType() switch
    {
        DataType.Null => "NULL",
        DataType.Void => "VOID",
        DataType.Integer => AsIntegerUnsafe.ToString(format, CultureInfo.InvariantCulture),
        DataType.String => AsStringUnsafe,
        DataType.Boolean => AsBooleanUnsafe.ToString(),
        DataType.Float => AsFloatUnsafe.ToString(format, CultureInfo.InvariantCulture),
        DataType.Numeric => AsNumeric.ToString(format, CultureInfo.InvariantCulture),
        DataType.Timestamp => AsTimestampUnsafe.ToString(format, CultureInfo.InvariantCulture),
        DataType.Interval => AsIntervalUnsafe.ToString(format, CultureInfo.InvariantCulture),
        DataType.Object => "object: " + AsObjectUnsafe,
        _ => "unknown"
    };

    /// <inheritdoc />
    public bool Equals(VariantValue other) => Equals(this, in other);
}
