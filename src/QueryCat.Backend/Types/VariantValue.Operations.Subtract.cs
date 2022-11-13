namespace QueryCat.Backend.Types;

public partial struct VariantValue
{
    public static UnaryFunction GetNegationDelegate(DataType leftType)
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

    public static VariantValue Subtract(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
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

    public static BinaryFunction GetSubtractDelegate(DataType leftType, DataType rightType)
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

    public static VariantValue Mul(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        VariantValue result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsInteger * right.AsInteger),
                DataType.Float => new VariantValue(left.AsInteger * right.AsFloat),
                DataType.Numeric => new VariantValue(left.AsInteger * right.AsNumeric),
                _ => Null,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsFloat * right.AsInteger),
                DataType.Float => new VariantValue(left.AsFloat * right.AsFloat),
                _ => Null,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsNumeric * right.AsInteger),
                DataType.Numeric => new VariantValue(left.AsNumeric * right.AsNumeric),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }
}
