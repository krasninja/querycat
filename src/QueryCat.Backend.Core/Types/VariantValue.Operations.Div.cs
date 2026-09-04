namespace QueryCat.Backend.Core.Types;

public partial struct VariantValue
{
    public static VariantValue Div(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var function = GetDivDelegate(left.Type, right.Type);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(in left, in right);
    }

    internal static BinaryFunction GetDivDelegate(DataType leftType, DataType rightType)
    {
        return leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => (in left, in right) =>
                {
                    var divisor = right.AsIntegerUnsafe;
                    return divisor != 0 ? new VariantValue(left.AsIntegerUnsafe / divisor) : Null;
                },
                DataType.Float => (in left, in right) =>
                {
                    var divisor = right.AsFloatUnsafe;
                    return divisor != 0.0 ? new VariantValue(left.AsIntegerUnsafe / divisor) : Null;
                },
                DataType.Numeric => (in left, in right) =>
                {
                    var divisor = right.AsNumericUnsafe;
                    return divisor != 0 ? new VariantValue(left.AsIntegerUnsafe / divisor) : Null;
                },
                _ => BinaryNullDelegate,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => (in left, in right) =>
                {
                    var divisor = right.AsIntegerUnsafe;
                    return divisor != 0 ? new VariantValue(left.AsFloatUnsafe / divisor) : Null;
                },
                DataType.Float => (in left, in right) =>
                {
                    var divisor = right.AsFloatUnsafe;
                    return divisor != 0.0 ? new VariantValue(left.AsFloatUnsafe / divisor) : Null;
                },
                _ => BinaryNullDelegate,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => (in left, in right) =>
                {
                    var divisor = right.AsIntegerUnsafe;
                    return divisor != 0 ? new VariantValue(left.AsNumericUnsafe / divisor) : Null;
                },
                DataType.Numeric => (in left, in right) =>
                {
                    var divisor = right.AsNumericUnsafe;
                    return divisor != 0 ? new VariantValue(left.AsNumericUnsafe / divisor) : Null;
                },
                _ => BinaryNullDelegate,
            },
            DataType.Interval => rightType switch
            {
                DataType.Integer => (in left, in right) =>
                {
                    var divisor = right.AsIntegerUnsafe;
                    return divisor != 0 ? new VariantValue(left.AsIntervalUnsafe / divisor) : Null;
                },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }
}
