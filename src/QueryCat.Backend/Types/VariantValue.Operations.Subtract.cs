namespace QueryCat.Backend.Types;

public partial struct VariantValue
{
    internal static UnaryFunction GetNegationDelegate(DataType leftType)
    {
        return leftType switch
        {
            DataType.Integer => (ref VariantValue left) =>
            {
                if (left.IsNull)
                {
                    return Null;
                }
                return new VariantValue(-left.AsIntegerUnsafe);
            },
            DataType.Float => (ref VariantValue left) =>
            {
                if (left.IsNull)
                {
                    return Null;
                }
                return new VariantValue(-left.AsFloatUnsafe);
            },
            DataType.Numeric => (ref VariantValue left) =>
            {
                if (left.IsNull)
                {
                    return Null;
                }
                return new VariantValue(-left.AsNumericUnsafe);
            },
            DataType.Boolean => (ref VariantValue left) =>
            {
                if (left.IsNull)
                {
                    return Null;
                }
                return new VariantValue(!left.AsBooleanUnsafe);
            },
            DataType.Interval => (ref VariantValue left) =>
            {
                if (left.IsNull)
                {
                    return Null;
                }
                return new VariantValue(-left.AsIntervalUnsafe);
            },
            _ => UnaryNullDelegate
        };
    }

    internal static VariantValue Subtract(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var function = GetSubtractDelegate(leftType, rightType);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(ref left, ref right);
    }

    internal static BinaryFunction GetSubtractDelegate(DataType leftType, DataType rightType)
    {
        var negativeFunction = GetNegationDelegate(leftType);
        var addFunction = GetAddDelegate(leftType, rightType);
        if (negativeFunction == UnaryNullDelegate || addFunction == BinaryNullDelegate)
        {
            return BinaryNullDelegate;
        }

        return (ref VariantValue left, ref VariantValue right) =>
        {
            var negativeRight = negativeFunction.Invoke(ref right);
            return addFunction.Invoke(ref left, ref negativeRight);
        };
    }
}
