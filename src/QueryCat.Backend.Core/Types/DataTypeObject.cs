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

    public abstract bool TryInteger(in VariantValue value, out long result);

    public long AsInteger(in VariantValue value) => TryInteger(value, out var result)
        ? result
        : throw CreateInvalidVariantTypeException(DataType.Integer);

    public abstract bool TryFloat(in VariantValue value, out double result);

    public double AsFloat(in VariantValue value) => TryFloat(value, out var result)
        ? result
        : throw CreateInvalidVariantTypeException(DataType.Float);

    public abstract bool TryNumeric(in VariantValue value, out decimal result);

    public decimal AsNumeric(in VariantValue value) => TryNumeric(value, out var result)
        ? result
        : throw CreateInvalidVariantTypeException(DataType.Numeric);

    public abstract bool TryTimestamp(in VariantValue value, out DateTime result);

    public DateTime AsTimestamp(in VariantValue value) => TryTimestamp(value, out var result)
        ? result
        : throw CreateInvalidVariantTypeException(DataType.Timestamp);

    public abstract bool TryInterval(in VariantValue value, out TimeSpan result);

    public TimeSpan AsInterval(in VariantValue value) => TryInterval(value, out var result)
        ? result
        : throw CreateInvalidVariantTypeException(DataType.Interval);

    public abstract bool TryBoolean(in VariantValue value, out bool result);

    public bool AsBoolean(in VariantValue value) => TryBoolean(value, out var result)
        ? result
        : throw CreateInvalidVariantTypeException(DataType.Boolean);

    public abstract bool TryString(in VariantValue value, out string result);

    public string AsString(in VariantValue value) => TryString(value, out var result)
        ? result
        : throw CreateInvalidVariantTypeException(DataType.String);

    public abstract bool TryBlob(in VariantValue value, out IBlobData result);

    public IBlobData AsBlob(in VariantValue value) => TryBlob(value, out var result)
        ? result
        : throw CreateInvalidVariantTypeException(DataType.Blob);

    protected InvalidVariantTypeException CreateInvalidVariantTypeException(DataType targetType) =>
        new(DataType, targetType);

    public bool TryConvert(in VariantValue value, DataType targetType, out VariantValue result)
    {
        var success = false;
        if (targetType == DataType.String)
        {
            success = TryString(value, out var outResult);
            result = new VariantValue(outResult);
            return success;
        }
        if (targetType == DataType.Integer)
        {
            success = TryInteger(value, out var outResult);
            result = new VariantValue(outResult);
            return success;
        }
        if (targetType == DataType.Float)
        {
            success = TryFloat(value, out var outResult);
            result = new VariantValue(outResult);
            return success;
        }
        if (targetType == DataType.Numeric)
        {
            success = TryNumeric(value, out var outResult);
            result = new VariantValue(outResult);
            return success;
        }
        if (targetType == DataType.Boolean)
        {
            success = TryBoolean(value, out var outResult);
            result = new VariantValue(outResult);
            return success;
        }
        if (targetType == DataType.Timestamp)
        {
            success = TryTimestamp(value, out var outResult);
            result = new VariantValue(outResult);
            return success;
        }
        if (targetType == DataType.Interval)
        {
            success = TryInterval(value, out var outResult);
            result = new VariantValue(outResult);
            return success;
        }
        if (targetType == DataType.Blob)
        {
            success = TryBlob(value, out var outResult);
            result = new VariantValue(outResult);
            return success;
        }
        result = VariantValue.Null;
        return success;
    }

    /// <inheritdoc />
    public override string ToString() => $"[{DataType}]";
}
