namespace QueryCat.Backend.Types;

public partial struct VariantValue
{
    internal static UnaryFunction GetNegationDelegate(DataType leftType)
    {
        return leftType switch
        {
            DataType.Integer => (in VariantValue left) =>
            {
                if (left.IsNull)
                {
                    return Null;
                }
                return new VariantValue(-left.AsIntegerUnsafe);
            },
            DataType.Float => (in VariantValue left) =>
            {
                if (left.IsNull)
                {
                    return Null;
                }
                return new VariantValue(-left.AsFloatUnsafe);
            },
            DataType.Numeric => (in VariantValue left) =>
            {
                if (left.IsNull)
                {
                    return Null;
                }
                return new VariantValue(-left.AsNumericUnsafe);
            },
            DataType.Boolean => (in VariantValue left) =>
            {
                if (left.IsNull)
                {
                    return Null;
                }
                return new VariantValue(!left.AsBooleanUnsafe);
            },
            DataType.Interval => (in VariantValue left) =>
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

    internal static VariantValue Subtract(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
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
        return function.Invoke(in left, in right);
    }

    internal static BinaryFunction GetSubtractDelegate(DataType leftType, DataType rightType)
    {
        var negativeFunction = GetNegationDelegate(leftType);
        var addFunction = GetAddDelegate(leftType, rightType);
        if (negativeFunction == UnaryNullDelegate || addFunction == BinaryNullDelegate)
        {
            return BinaryNullDelegate;
        }

        return (in VariantValue left, in VariantValue right) =>
        {
            var negativeRight = negativeFunction.Invoke(in right);
            return addFunction.Invoke(in left, in negativeRight);
        };
    }
}
