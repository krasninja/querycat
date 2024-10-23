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
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe / right.AsIntegerUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe / right.AsFloatUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe / right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe / right.AsIntegerUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe / right.AsFloatUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe / right.AsIntegerUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe / right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Interval => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntervalUnsafe / right.AsIntegerUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }
}
