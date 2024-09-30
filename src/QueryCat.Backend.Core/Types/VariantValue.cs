using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace QueryCat.Backend.Core.Types;

/// <summary>
/// The union-like class. It holds only one value of the specified data type.
/// In fact it has two 64-bit fields.
/// </summary>
public readonly partial struct VariantValue : IEquatable<VariantValue>
{
    public const string TrueValueString = "TRUE";
    public const string FalseValueString = "FALSE";
    public const string NullValueString = "NULL";
    public const string VoidValueString = "VOID";
    private const string UnknownString = "[unknown]";

    public const string FloatNumberFormat = "F";

    private static readonly DataTypeObject IntegerObject = new(DataType.Integer);
    private static readonly DataTypeObject FloatObject = new(DataType.Float);
    private static readonly DataTypeObject TimestampObject = new(DataType.Timestamp);
    private static readonly DataTypeObject IntervalObject = new(DataType.Interval);
    private static readonly DataTypeObject BooleanObject = new(DataType.Boolean);

    public static VariantValue OneIntegerValue = new(1);
    public static VariantValue TrueValue = new(true);
    public static VariantValue FalseValue = new(false);

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct TypeUnion : IEquatable<TypeUnion>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal TypeUnion(long value) : this()
        {
            IntegerValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal TypeUnion(double value) : this()
        {
            DoubleValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal TypeUnion(DateTime value) : this()
        {
            DateTimeValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal TypeUnion(bool value) : this()
        {
            BooleanValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal TypeUnion(TimeSpan value) : this()
        {
            TimeSpanValue = value;
        }

        /// <inheritdoc />
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
            => obj is TypeUnion typeUnion && typeUnion.IntegerValue == this.IntegerValue;

        /// <inheritdoc />
        public override int GetHashCode() => IntegerValue.GetHashCode();

        /// <inheritdoc />
        public bool Equals(TypeUnion other) => IntegerValue == other.IntegerValue;
    }

    private readonly TypeUnion _valueUnion;

    private readonly object? _object = null;

    /// <summary>
    /// NULL value.
    /// </summary>
    public static VariantValue Null = default;

    /// <summary>
    /// Variant value type.
    /// </summary>
    public DataType Type => _object is DataTypeObject dataTypeObject
        ? dataTypeObject.DataType
        : (DataType)_valueUnion.IntegerValue;

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
            DataType.Blob => StreamBlobData.Empty,
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
        _valueUnion = new TypeUnion((int)DataType.String);
    }

    public VariantValue(char value)
    {
        _object = value.ToString();
        _valueUnion = default;
    }

    public VariantValue(ReadOnlySpan<char> value)
    {
        _object = value.ToString();
        _valueUnion = new TypeUnion((int)DataType.String);
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

    public VariantValue(DateTimeOffset value)
    {
        _valueUnion = new TypeUnion(value.DateTime);
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
        _valueUnion = new TypeUnion((int)DataType.Numeric);
    }

    private VariantValue(object obj)
    {
        _object = obj;
        _valueUnion = new TypeUnion((int)DataType.Object);
    }

    private VariantValue(IBlobData blob)
    {
        _object = blob;
        _valueUnion = new TypeUnion((int)DataType.Blob);
    }

    private VariantValue(byte[] bytes)
    {
        _object = new StreamBlobData(() => new MemoryStream(bytes, writable: false));
        _valueUnion = new TypeUnion((int)DataType.Blob);
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
        if (obj is string || typeof(T).IsEnum || obj is char)
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
        if (obj is Enum)
        {
            return new VariantValue(obj.ToString());
        }
        if (obj is JsonValue jsonValue && TryGetValueFromJsonValue(jsonValue, out var jsonResult))
        {
            return jsonResult;
        }
        if (obj is JsonNode jsonNode)
        {
            return new VariantValue(jsonNode.ToJsonString());
        }
        if (obj is IBlobData blobData)
        {
            return new VariantValue(blobData);
        }
        if (obj is Guid guid)
        {
            return new VariantValue(guid.ToString());
        }
        return new VariantValue(obj);
    }

    internal static bool TryGetValueFromJsonValue(JsonValue jsonValue, out VariantValue value)
    {
        var jsonType = jsonValue.GetValue<JsonElement>().ValueKind;
        if (jsonType == JsonValueKind.Number)
        {
            if (jsonValue.TryGetValue(out long jsonLongValue))
            {
                value = new VariantValue(jsonLongValue);
                return true;
            }
            if (jsonValue.TryGetValue(out decimal jsonDecimalValue))
            {
                value = new VariantValue(jsonDecimalValue);
                return true;
            }
            if (jsonValue.TryGetValue(out double jsonDoubleValue))
            {
                value = new VariantValue(jsonDoubleValue);
                return true;
            }
        }

        if (jsonType == JsonValueKind.String && jsonValue.TryGetValue(out string? jsonStringValue))
        {
            value = new VariantValue(jsonStringValue);
            return true;
        }
        if (jsonType == JsonValueKind.True)
        {
            value = TrueValue;
            return true;
        }
        if (jsonType == JsonValueKind.False)
        {
            value = FalseValue;
            return true;
        }
        if (jsonType == JsonValueKind.Null || jsonType == JsonValueKind.Undefined)
        {
            value = Null;
            return true;
        }

        value = Null;
        return false;
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

    public IBlobData AsBlob => (IBlobData)CheckTypeAndTryToCast(DataType.Blob)._object!;

    internal IBlobData AsBlobUnsafe => (IBlobData)_object!;

    #endregion

    /// <summary>
    /// Get object of the specified type.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    /// <returns>Object.</returns>
    public T As<T>()
    {
        var retType = typeof(T);

        if (retType == typeof(Int64)
            || retType == typeof(Int32)
            || retType == typeof(Int16)
            || retType == typeof(Byte)
            || retType == typeof(UInt64)
            || retType == typeof(UInt32)
            || retType == typeof(UInt16)
            || retType == typeof(SByte))
        {
            return (T)Convert.ChangeType(AsInteger, typeof(T));
        }
        if (retType == typeof(bool))
        {
            return (T)Convert.ChangeType(AsBoolean, typeof(T));
        }
        if (retType == typeof(double) || retType == typeof(float))
        {
            return (T)Convert.ChangeType(AsFloat, typeof(T));
        }
        if (retType == typeof(decimal))
        {
            return (T)Convert.ChangeType(AsNumeric, typeof(T));
        }
        if (retType == typeof(string) || retType == typeof(char))
        {
            return (T)Convert.ChangeType(AsString, typeof(T));
        }
        if (retType == typeof(DateTime) || retType == typeof(DateTimeOffset))
        {
            return (T)Convert.ChangeType(AsTimestamp, typeof(T));
        }
        if (retType == typeof(TimeSpan))
        {
            return (T)Convert.ChangeType(AsInterval, typeof(T));
        }

        var sourceObj = CheckTypeAndTryToCast(DataType.Object)._object;
        if (sourceObj == null)
        {
            throw new InvalidOperationException($"Object is null. Target type is '{typeof(T).Name}'.");
        }
        if (sourceObj is T obj)
        {
            return obj;
        }
        throw new InvalidOperationException(
            $"Cannot cast object of type '{sourceObj.GetType().Name}' to type '{typeof(T).Name}'.");
    }

    /// <summary>
    /// Get the internal value as .NET object.
    /// </summary>
    /// <returns>Value object.</returns>
    public object? GetGenericObject() => Type switch
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
        if (targetType != Type)
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
        success = long.TryParse(value, NumberStyles.Any, Application.Culture, out var @out);
        if (!success)
        {
            // Try HEX format.
            success = value.StartsWith("0x") &&
                long.TryParse(value[2..], NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier,
                    Application.Culture, out @out);
        }
        return success ? new VariantValue(@out) : Null;
    }

    private static VariantValue StringToTimestamp(string? value, out bool success)
        => StringToTimestamp((ReadOnlySpan<char>)value, out success);

    private static readonly string[] _dateTimeAdditionalFormats =
    {
        "yyMMdd",
        "yyyyMMdd"
    };

    private static VariantValue StringToTimestamp(in ReadOnlySpan<char> value, out bool success)
    {
        success = DateTime.TryParse(value, Application.Culture, DateTimeStyles.None, out var @out);
        if (!success)
        {
            success = DateTime.TryParseExact(value, _dateTimeAdditionalFormats, null,
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
        success = double.TryParse(value, NumberStyles.Any, Application.Culture, out var @out);
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

    private static VariantValue StringToNumeric(in string? value, out bool success)
        => StringToNumeric((ReadOnlySpan<char>)value, out success);

    private static VariantValue StringToNumeric(in ReadOnlySpan<char> value, out bool success)
    {
        success = decimal.TryParse(value, NumberStyles.Any, Application.Culture, out var @out);
        return success ? new VariantValue(@out) : Null;
    }

    private static VariantValue StringToString(in string? value, out bool success)
    {
        success = true;
        return new VariantValue(value);
    }

    private static VariantValue StringToString(in ReadOnlySpan<char> value, out bool success)
    {
        success = true;
        return new VariantValue(value);
    }

    private static VariantValue StringToBlob(in string? value, out bool success)
    {
        success = true;

        var bytes = System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty);
        var blobData = new StreamBlobData(bytes);
        return new VariantValue(blobData);
    }

    private static VariantValue BlobToString(IBlobData? value, out bool success)
    {
        success = true;

        value ??= StreamBlobData.Empty;
        var sr = new StreamReader(value.GetStream());
        return new VariantValue(sr.ReadToEnd());
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
        // Any type is dynamic.
        if (targetType == DataType.Dynamic)
        {
            output = this;
            return true;
        }

        var sourceType = Type;
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
                DataType.Blob => StringToBlob(_object as string, out success),
                _ => Null
            },
            DataType.Float => targetType switch
            {
                DataType.Integer => new((int)_valueUnion.DoubleValue),
                DataType.String => new(_valueUnion.DoubleValue.ToString(Application.Culture)),
                DataType.Numeric => new((decimal)_valueUnion.DoubleValue),
                _ => Null
            },
            DataType.Timestamp => targetType switch
            {
                DataType.String => new(_valueUnion.DateTimeValue.ToString(Application.Culture)),
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
                DataType.String => new(((decimal)_object!).ToString(Application.Culture)),
                DataType.Float => new(decimal.ToDouble((decimal)_object!)),
                _ => Null
            },
            DataType.Blob => targetType switch
            {
                DataType.String => BlobToString((IBlobData?)_object, out success),
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
            throw new InvalidVariantTypeException(Type, targetType);
        }
    }

    /// <summary>
    /// Try to create variant value from string.
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
    public override string ToString() => Type switch
    {
        DataType.Null => NullValueString,
        DataType.Void => VoidValueString,
        DataType.Dynamic => VoidValueString,
        DataType.Integer => AsIntegerUnsafe.ToString(Application.Culture),
        DataType.String => AsStringUnsafe,
        DataType.Boolean => AsBooleanUnsafe.ToString(Application.Culture),
        DataType.Float => AsFloatUnsafe.ToString(FloatNumberFormat, Application.Culture),
        DataType.Numeric => AsNumericUnsafe.ToString(FloatNumberFormat, Application.Culture),
        DataType.Timestamp => AsTimestampUnsafe.ToString(Application.Culture),
        DataType.Interval => AsIntervalUnsafe.ToString("c", Application.Culture),
        DataType.Object => $"[object:{AsObjectUnsafe}]",
        DataType.Blob => BlobToShortString(AsBlobUnsafe),
        _ => UnknownString,
    };

    /// <summary>
    /// Convert to string according to the format.
    /// </summary>
    /// <param name="format">Format string.</param>
    /// <returns>String representation.</returns>
    public string ToString(string format) => Type switch
    {
        DataType.Null => NullValueString,
        DataType.Void => VoidValueString,
        DataType.Dynamic => VoidValueString,
        DataType.Integer => AsIntegerUnsafe.ToString(format, Application.Culture),
        DataType.String => AsStringUnsafe,
        DataType.Boolean => AsBooleanUnsafe.ToString(),
        DataType.Float => AsFloatUnsafe.ToString(format, Application.Culture),
        DataType.Numeric => AsNumeric.ToString(format, Application.Culture),
        DataType.Timestamp => AsTimestampUnsafe.ToString(format, Application.Culture),
        DataType.Interval => AsIntervalUnsafe.ToString(format, Application.Culture),
        DataType.Object => $"[object:{AsObjectUnsafe}]",
        DataType.Blob => BlobToShortString(AsBlobUnsafe),
        _ => UnknownString,
    };

    /// <summary>
    /// Convert to string according to the format.
    /// </summary>
    /// <param name="formatProvider">Culture specific formatting information.</param>
    /// <returns>String representation.</returns>
    public string ToString(IFormatProvider? formatProvider) => Type switch
    {
        DataType.Null => NullValueString,
        DataType.Void => VoidValueString,
        DataType.Integer => AsIntegerUnsafe.ToString(formatProvider),
        DataType.String => AsStringUnsafe,
        DataType.Boolean => AsBooleanUnsafe.ToString(),
        DataType.Float => AsFloatUnsafe.ToString(formatProvider),
        DataType.Numeric => AsNumeric.ToString(formatProvider),
        DataType.Timestamp => AsTimestampUnsafe.ToString(formatProvider),
        DataType.Interval => AsIntervalUnsafe.ToString(null, formatProvider),
        DataType.Object => $"[object:{AsObjectUnsafe}]",
        DataType.Blob => BlobToShortString(AsBlobUnsafe),
        _ => UnknownString,
    };

    private static string BlobToShortString(IBlobData blobData)
    {
        // Convert BLOB into string: ABC\5C.
        var sb = new StringBuilder((int)blobData.Length * 3);
        using var stream = blobData.GetStream();
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            int read;
            var oneByteArr = new byte[1];
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                foreach (var b in buffer.AsSpan(0, read))
                {
                    var ch = (char)b;
                    if (char.IsAsciiLetterOrDigit(ch) || char.IsPunctuation(ch)
                        || char.IsSeparator(ch))
                    {
                        sb.Append(ch);
                    }
                    else
                    {
                        oneByteArr[0] = b;
                        sb.Append("\\x")
                            .Append(Convert.ToHexString(oneByteArr));
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public bool Equals(VariantValue other) => Equals(this, in other);
}
