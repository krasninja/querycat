namespace QueryCat.Backend.Types;

public partial struct VariantValue
{
    public static VariantValue Add(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var function = GetAddDelegate(leftType, rightType);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(ref left, ref right);
    }

    public static BinaryFunction GetAddDelegate(DataType leftType, DataType rightType)
    {
        return leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe + right.AsIntegerUnsafe);
                },
                DataType.Float => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe + right.AsFloatUnsafe);
                },
                DataType.Numeric => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsInteger + right.AsNumeric);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsFloat + right.AsInteger);
                },
                DataType.Float => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsFloat + right.AsFloat);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsNumeric + right.AsInteger);
                },
                DataType.Numeric => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsNumeric + right.AsNumeric);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Interval => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsTimestamp + right.AsInterval);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Interval => rightType switch
            {
                DataType.Interval => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsInterval + right.AsInterval);
                },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }
}
