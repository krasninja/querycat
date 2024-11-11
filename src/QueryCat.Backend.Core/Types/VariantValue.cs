using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace QueryCat.Backend.Core.Types;

/// <summary>
/// The union-like class. It holds only one value of the specified data type.
/// In fact, it has two 64-bit fields.
/// </summary>
public readonly partial struct VariantValue :
    IEquatable<VariantValue>,
    IConvertible,
    ICloneable
{
    public const string TrueValueString = "TRUE";
    public const string FalseValueString = "FALSE";
    public const string NullValueString = "NULL";
    public const string VoidValueString = "VOID";
    private const string UnknownString = "[unknown]";

    public const string FloatNumberFormat = "F";

    private static readonly DataTypeObject _integerObject = IntegerDataTypeObject.Instance;
    private static readonly DataTypeObject _floatObject = FloatDataTypeObject.Instance;
    private static readonly DataTypeObject _timestampObject = TimestampDataTypeObject.Instance;
    private static readonly DataTypeObject _intervalObject = IntervalDataTypeObject.Instance;
    private static readonly DataTypeObject _booleanObject = BooleanDataTypeObject.Instance;

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
            DataType.Integer => _integerObject,
            DataType.String => string.Empty,
            DataType.Boolean => _booleanObject,
            DataType.Float => _floatObject,
            DataType.Timestamp => _timestampObject,
            DataType.Interval => _intervalObject,
            DataType.Object => null,
            DataType.Blob => StreamBlobData.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
        _valueUnion = default;
    }

    public VariantValue(long value)
    {
        _valueUnion = new TypeUnion(value);
        _object = _integerObject;
    }

    public VariantValue(long? value)
    {
        if (!value.HasValue)
        {
            _object = null;
        }
        else
        {
            _valueUnion = new TypeUnion(value.Value);
            _object = _integerObject;
        }
    }

    public VariantValue(int value)
    {
        _valueUnion = new TypeUnion(value);
        _object = _integerObject;
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
        _object = _floatObject;
    }

    public VariantValue(double? value)
    {
        if (!value.HasValue)
        {
            _object = null;
        }
        else
        {
            _valueUnion = new TypeUnion(value.Value);
            _object = _floatObject;
        }
    }

    public VariantValue(DateTime value)
    {
        _valueUnion = new TypeUnion(value);
        _object = _timestampObject;
    }

    public VariantValue(DateTime? value)
    {
        if (!value.HasValue)
        {
            _object = null;
        }
        else
        {
            _valueUnion = new TypeUnion(value.Value);
            _object = _timestampObject;
        }
    }

    public VariantValue(DateTimeOffset value)
    {
        _valueUnion = new TypeUnion(value.DateTime);
        _object = _timestampObject;
    }

    public VariantValue(TimeSpan value)
    {
        _valueUnion = new TypeUnion(value);
        _object = _intervalObject;
    }

    public VariantValue(TimeSpan? value)
    {
        if (!value.HasValue)
        {
            _object = null;
        }
        else
        {
            _valueUnion = new TypeUnion(value.Value);
            _object = _intervalObject;
        }
    }

    public VariantValue(bool value)
    {
        _valueUnion = new TypeUnion(value);
        _object = _booleanObject;
    }

    public VariantValue(bool? value)
    {
        if (!value.HasValue)
        {
            _object = null;
        }
        else
        {
            _valueUnion = new TypeUnion(value.Value);
            _object = _booleanObject;
        }
    }

    public VariantValue(decimal value)
    {
        _object = value;
        _valueUnion = new TypeUnion((int)DataType.Numeric);
    }

    public VariantValue(decimal? value)
    {
        if (!value.HasValue)
        {
            _object = null;
        }
        else
        {
            _object = value.Value;
            _valueUnion = new TypeUnion((int)DataType.Numeric);
        }
    }

    private VariantValue(object obj)
    {
        _object = obj;
        _valueUnion = new TypeUnion((int)DataType.Object);
    }

    public VariantValue(IBlobData? blob)
    {
        _object = blob;
        _valueUnion = new TypeUnion((int)DataType.Blob);
    }

    private VariantValue(byte[] bytes)
    {
        _object = new StreamBlobData(() => new MemoryStream(bytes, writable: false));
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="value">Value.</param>
    public VariantValue(VariantValue value)
    {
        _object = value._object;
        _valueUnion = value._valueUnion;
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
        _object == _integerObject || _object == _floatObject ||
        _object == _booleanObject || _object == _timestampObject ||
        _object == _intervalObject;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private DataTypeObject GetDataTypeObject()
    {
        if (_object is DataTypeObject dataTypeObject)
        {
            return dataTypeObject;
        }
        return Type switch
        {
            DataType.String => StringDataTypeObject.Instance,
            DataType.Numeric => NumericDataTypeObject.Instance,
            DataType.Blob => BlobDataTypeObject.Instance,
            DataType.Object => ObjectDataTypeObject.Instance,
            DataType.Dynamic => ObjectDataTypeObject.Instance,
            _ => NullDataTypeObject.Instance,
        };
    }

    #endregion

    #region Getters and Setters

    public long? AsInteger => GetDataTypeObject().ToInteger(in this);

    internal long AsIntegerUnsafe => _valueUnion.IntegerValue;

    public string AsString => GetDataTypeObject().ToString(in this);

    internal string AsStringUnsafe => _object != null ? (string)_object : string.Empty;

    public double? AsFloat => GetDataTypeObject().ToFloat(in this);

    internal double AsFloatUnsafe => _valueUnion.DoubleValue;

    public DateTime? AsTimestamp => GetDataTypeObject().ToTimestamp(in this);

    internal DateTime AsTimestampUnsafe => _valueUnion.DateTimeValue;

    public TimeSpan? AsInterval => GetDataTypeObject().ToInterval(in this);

    internal TimeSpan AsIntervalUnsafe => _valueUnion.TimeSpanValue;

    public bool AsBoolean => GetDataTypeObject().ToBoolean(in this) ?? false;

    public bool? AsBooleanNullable => GetDataTypeObject().ToBoolean(in this);

    internal bool AsBooleanUnsafe => _valueUnion.BooleanValue;

    public decimal? AsNumeric => GetDataTypeObject().ToNumeric(in this);

    internal decimal AsNumericUnsafe => (decimal)_object!;

    public object? AsObject => Type == DataType.Object ? _object : null;

    internal object? AsObjectUnsafe => _object;

    public IBlobData? AsBlob => GetDataTypeObject().ToBlob(in this);

    internal IBlobData AsBlobUnsafe => (IBlobData)_object!;

    #endregion

    /// <summary>
    /// Get object of the specified type. The exception is thrown if it cannot be resolved or null.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    /// <returns>Object.</returns>
    public T AsRequired<T>()
    {
        var obj = As<T>();
        if (obj == null)
        {
            throw new InvalidOperationException(string.Format(Resources.Errors.ObjectIsNull, typeof(T).Name));
        }
        return obj;
    }

    /// <summary>
    /// Get object of the specified type.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    /// <returns>Object.</returns>
    public T? As<T>()
    {
        var retType = typeof(T);
        if (IsNull)
        {
            return default;
        }

        if (retType == typeof(Int64)
            || retType == typeof(Int32)
            || retType == typeof(Int16)
            || retType == typeof(Byte)
            || retType == typeof(UInt64)
            || retType == typeof(UInt32)
            || retType == typeof(UInt16)
            || retType == typeof(SByte))
        {
            return (T?)Convert.ChangeType(AsInteger, typeof(T?));
        }
        if (retType == typeof(bool))
        {
            return (T?)Convert.ChangeType(AsBoolean, typeof(T?));
        }
        if (retType == typeof(double) || retType == typeof(float))
        {
            return (T?)Convert.ChangeType(AsFloat, typeof(T?));
        }
        if (retType == typeof(decimal))
        {
            return (T?)Convert.ChangeType(AsNumeric, typeof(T?));
        }
        if (retType == typeof(string) || retType == typeof(char))
        {
            return (T)Convert.ChangeType(AsString, typeof(T?));
        }
        if (retType == typeof(DateTime) || retType == typeof(DateTimeOffset))
        {
            return (T?)Convert.ChangeType(AsTimestamp, typeof(T?));
        }
        if (retType == typeof(TimeSpan))
        {
            return (T?)Convert.ChangeType(AsInterval, typeof(T?));
        }

        var sourceObj = Cast(DataType.Object)._object;
        if (sourceObj == null)
        {
            return default;
        }
        if (sourceObj is T obj)
        {
            return obj;
        }
        throw new InvalidOperationException(
            string.Format(Resources.Errors.ObjectInvalidType, sourceObj.GetType().Name, typeof(T).Name));
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

    /// <summary>
    /// Determines whether the value is null.
    /// </summary>
    public bool IsNull => _object == null;

    #region Casting

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
        if (Type == targetType || targetType == DataType.Dynamic)
        {
            output = this;
            return true;
        }

        return GetDataTypeObject().TryConvert(in this, targetType, out output);
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
            DataType.Integer => new VariantValue(StringDataTypeObject.StringToInteger(value, out success)),
            DataType.Float => new VariantValue(StringDataTypeObject.StringToFloat(value, out success)),
            DataType.Timestamp => new VariantValue(StringDataTypeObject.StringToTimestamp(value, out success)),
            DataType.Boolean => new VariantValue(StringDataTypeObject.StringToBoolean(value, out success)),
            DataType.Numeric => new VariantValue(StringDataTypeObject.StringToNumeric(value, out success)),
            DataType.String => new VariantValue(StringDataTypeObject.StringToString(value, out success)),
            DataType.Interval => new VariantValue(StringDataTypeObject.StringToInterval(value, out success)),
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

    public static implicit operator decimal?(VariantValue value) => value.AsNumeric;

    public static implicit operator int?(VariantValue value) => (int?)value.AsInteger;

    public static implicit operator long?(VariantValue value) => value.AsInteger;

    public static implicit operator bool?(VariantValue value) => value.AsBooleanNullable;

    public static implicit operator double?(VariantValue value) => value.AsFloat;

    public static implicit operator string(VariantValue value) => value.AsString;

    public static implicit operator DateTime?(VariantValue value) => value.AsTimestamp;

    public static implicit operator TimeSpan?(VariantValue value) => value.AsInterval;

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

    /// <inheritdoc />
    public object Clone() => new VariantValue(this);

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
        DataType.Boolean => AsBooleanUnsafe.ToString(Application.Culture),
        DataType.Float => AsFloatUnsafe.ToString(format, Application.Culture),
        DataType.Numeric => AsNumericUnsafe.ToString(format, Application.Culture),
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
        DataType.Boolean => AsBooleanUnsafe.ToString(formatProvider),
        DataType.Float => AsFloatUnsafe.ToString(FloatNumberFormat, formatProvider),
        DataType.Numeric => AsNumericUnsafe.ToString(FloatNumberFormat, formatProvider),
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
