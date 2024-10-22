namespace QueryCat.Backend.Core.Types;

/// <summary>
/// The special object just to indicate type in <see cref="VariantValue" /> struct.
/// </summary>
internal abstract class DataTypeObject
{
    /// <summary>
    /// Related type.
    /// </summary>
    public DataType DataType { get; }

    /// <summary>
    /// Type parameter.
    /// </summary>
    public string TypeParam { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dataType">Data type.</param>
    /// <param name="typeParam">Type parameter.</param>
    public DataTypeObject(DataType dataType, string? typeParam = null)
    {
        DataType = dataType;
        TypeParam = typeParam ?? string.Empty;
    }

    #region Integer

    public virtual bool CanToInteger { get; } = false;

    public virtual long? ToInteger(in VariantValue value) => null;

    public long AsInteger(in VariantValue value)
    {
        if (CanToInteger)
        {
            var result = ToInteger(in value);
            if (result.HasValue)
            {
                return result.Value;
            }
        }
        throw CreateInvalidVariantTypeException(DataType.Integer);
    }

    #endregion

    #region Float

    public virtual bool CanToFloat { get; } = false;

    public virtual double? ToFloat(in VariantValue value) => null;

    public double AsFloat(in VariantValue value)
    {
        if (CanToFloat)
        {
            var result = ToFloat(in value);
            if (result.HasValue)
            {
                return result.Value;
            }
        }
        throw CreateInvalidVariantTypeException(DataType.Float);
    }

    #endregion

    #region Numeric

    public virtual bool CanToNumeric { get; } = false;

    public virtual decimal? ToNumeric(in VariantValue value) => null;

    public decimal AsNumeric(in VariantValue value)
    {
        if (CanToNumeric)
        {
            var result = ToNumeric(in value);
            if (result.HasValue)
            {
                return result.Value;
            }
        }
        throw CreateInvalidVariantTypeException(DataType.Numeric);
    }

    #endregion

    #region Timestamp

    public virtual bool CanToTimestamp { get; } = false;

    public virtual DateTime? ToTimestamp(in VariantValue value) => null;

    public DateTime AsTimestamp(in VariantValue value)
    {
        if (CanToTimestamp)
        {
            var result = ToTimestamp(in value);
            if (result.HasValue)
            {
                return result.Value;
            }
        }
        throw CreateInvalidVariantTypeException(DataType.Timestamp);
    }

    #endregion

    #region Interval

    public virtual bool CanToInterval { get; } = false;

    public virtual TimeSpan? ToInterval(in VariantValue value) => null;

    public TimeSpan AsInterval(in VariantValue value)
    {
        if (CanToInterval)
        {
            var result = ToInterval(in value);
            if (result.HasValue)
            {
                return result.Value;
            }
        }
        throw CreateInvalidVariantTypeException(DataType.Interval);
    }

    #endregion

    #region Boolean

    public virtual bool CanToBoolean { get; } = false;

    public virtual bool? ToBoolean(in VariantValue value) => null;

    public bool AsBoolean(in VariantValue value)
    {
        if (CanToBoolean)
        {
            var result = ToBoolean(in value);
            if (result.HasValue)
            {
                return result.Value;
            }
        }
        throw CreateInvalidVariantTypeException(DataType.Boolean);
    }

    #endregion

    #region String

    public virtual bool CanToString { get; } = true;

    public virtual string ToString(in VariantValue value) => value.ToString(Application.Culture);

    public string AsString(in VariantValue value) => CanToString
        ? ToString(in value)
        : throw CreateInvalidVariantTypeException(DataType.String);

    #endregion

    #region Blob

    public virtual bool CanToBlob { get; } = false;

    public virtual IBlobData? ToBlob(in VariantValue value) => null;

    #endregion

    protected InvalidVariantTypeException CreateInvalidVariantTypeException(DataType targetType) =>
        new(DataType, targetType);

    public bool TryConvert(in VariantValue value, DataType targetType, out VariantValue result)
    {
        if (targetType == DataType.String && CanToString)
        {
            result = new VariantValue(ToString(value));
            return true;
        }
        if (targetType == DataType.Integer && CanToInteger)
        {
            result = new VariantValue(ToInteger(value));
            return true;
        }
        if (targetType == DataType.Float && CanToFloat)
        {
            result = new VariantValue(ToFloat(value));
            return true;
        }
        if (targetType == DataType.Numeric && CanToNumeric)
        {
            result = new VariantValue(ToNumeric(value));
            return true;
        }
        if (targetType == DataType.Boolean && CanToBoolean)
        {
            result = new VariantValue(ToBoolean(value));
            return true;
        }
        if (targetType == DataType.Timestamp)
        {
            result = new VariantValue(ToTimestamp(value));
            return true;
        }
        if (targetType == DataType.Interval)
        {
            result = new VariantValue(ToInterval(value));
            return true;
        }
        if (targetType == DataType.Blob)
        {
            result = new VariantValue(ToBlob(value));
            return true;
        }
        result = VariantValue.Null;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => $"[{DataType}]";
}
