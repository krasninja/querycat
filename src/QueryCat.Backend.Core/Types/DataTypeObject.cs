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

    #endregion

    #region Float

    public virtual bool CanToFloat { get; } = false;

    public virtual double? ToFloat(in VariantValue value) => null;

    #endregion

    #region Numeric

    public virtual bool CanToNumeric { get; } = false;

    public virtual decimal? ToNumeric(in VariantValue value) => null;

    #endregion

    #region Timestamp

    public virtual bool CanToTimestamp { get; } = false;

    public virtual DateTime? ToTimestamp(in VariantValue value) => null;

    #endregion

    #region Interval

    public virtual bool CanToInterval { get; } = false;

    public virtual TimeSpan? ToInterval(in VariantValue value) => null;

    #endregion

    #region Boolean

    public virtual bool CanToBoolean { get; } = false;

    public virtual bool? ToBoolean(in VariantValue value) => null;

    #endregion

    #region String

    public virtual bool CanToString { get; } = false;

    public virtual string ToString(in VariantValue value) => string.Empty;

    #endregion

    #region Blob

    public virtual bool CanToBlob { get; } = false;

    public virtual IBlobData? ToBlob(in VariantValue value) => null;

    #endregion

    public bool TryConvert(in VariantValue value, DataType targetType, out VariantValue result)
    {
        if (targetType == DataType.String && CanToString)
        {
            result = new VariantValue(ToString(value));
            return !result.IsNull;
        }
        if (targetType == DataType.Integer && CanToInteger)
        {
            result = new VariantValue(ToInteger(value));
            return !result.IsNull;
        }
        if (targetType == DataType.Float && CanToFloat)
        {
            result = new VariantValue(ToFloat(value));
            return !result.IsNull;
        }
        if (targetType == DataType.Numeric && CanToNumeric)
        {
            result = new VariantValue(ToNumeric(value));
            return !result.IsNull;
        }
        if (targetType == DataType.Boolean && CanToBoolean)
        {
            result = new VariantValue(ToBoolean(value));
            return !result.IsNull;
        }
        if (targetType == DataType.Timestamp)
        {
            result = new VariantValue(ToTimestamp(value));
            return !result.IsNull;
        }
        if (targetType == DataType.Interval)
        {
            result = new VariantValue(ToInterval(value));
            return !result.IsNull;
        }
        if (targetType == DataType.Blob)
        {
            result = new VariantValue(ToBlob(value));
            return !result.IsNull;
        }
        result = VariantValue.Null;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => $"[{DataType}]";
}
