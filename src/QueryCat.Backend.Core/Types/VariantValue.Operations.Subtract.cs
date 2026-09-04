namespace QueryCat.Backend.Core.Types;

public partial struct VariantValue
{
    internal static UnaryFunction GetNegationDelegate(DataType leftType)
    {
        return leftType switch
        {
            DataType.Integer => (in left) =>
            {
                return new VariantValue(-left.AsIntegerUnsafe);
            },
            DataType.Float => (in left) =>
            {
                return new VariantValue(-left.AsFloatUnsafe);
            },
            DataType.Numeric => (in left) =>
            {
                return new VariantValue(-left.AsNumericUnsafe);
            },
            DataType.Boolean => (in left) =>
            {
                return new VariantValue(!left.AsBooleanUnsafe);
            },
            DataType.Interval => (in left) =>
            {
                return new VariantValue(-left.AsIntervalUnsafe);
            },
            _ => UnaryNullDelegate
        };
    }

    internal static VariantValue Subtract(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var function = GetSubtractDelegate(left.Type, right.Type);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(in left, in right);
    }

    internal static BinaryFunction GetSubtractDelegate(DataType leftType, DataType rightType)
    {
        return leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => (in left, in right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe - right.AsIntegerUnsafe);
                },
                DataType.Float => (in left, in right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe - right.AsFloatUnsafe);
                },
                DataType.Numeric => (in left, in right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe - right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => (in left, in right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe - right.AsIntegerUnsafe);
                },
                DataType.Float => (in left, in right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe - right.AsFloatUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => (in left, in right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe - right.AsIntegerUnsafe);
                },
                DataType.Numeric => (in left, in right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe - right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Interval => (in left, in right) =>
                {
                    return new VariantValue(left.AsTimestampUnsafe - right.AsIntervalUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Interval => rightType switch
            {
                DataType.Interval => (in left, in right) =>
                {
                    return new VariantValue(left.AsIntervalUnsafe - right.AsIntervalUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }
}
