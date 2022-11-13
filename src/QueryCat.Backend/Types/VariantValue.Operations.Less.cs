namespace QueryCat.Backend.Types;

public partial struct VariantValue
{
    internal static VariantValue Less(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var function = GetLessDelegate(leftType, rightType);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(ref left, ref right);
    }

    internal static BinaryFunction GetLessDelegate(DataType leftType, DataType rightType)
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
                    return new VariantValue(left.AsIntegerUnsafe < right.AsIntegerUnsafe);
                },
                DataType.Float => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe < right.AsFloatUnsafe);
                },
                DataType.Numeric => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe < right.AsNumericUnsafe);
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
                    return new VariantValue(left.AsFloatUnsafe < right.AsFloatUnsafe);
                },
                DataType.Float => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsFloatUnsafe < right.AsFloatUnsafe);
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
                    return new VariantValue(left.AsNumericUnsafe < right.AsIntegerUnsafe);
                },
                DataType.Numeric => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsNumericUnsafe < right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Boolean => rightType switch
            {
                DataType.Boolean or DataType.Integer => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe < right.AsIntegerUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.String => rightType switch
            {
                DataType.String => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(string.CompareOrdinal(left.AsStringUnsafe, right.AsStringUnsafe) < 0);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Timestamp => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsTimestampUnsafe < right.AsTimestampUnsafe);
                },
                DataType.String => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsTimestampUnsafe < right.AsTimestamp);
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
                    return new VariantValue(left.AsIntervalUnsafe < right.AsIntervalUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }

    internal static VariantValue LessOrEquals(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var function = GetLessOrEqualsDelegate(leftType, rightType);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(ref left, ref right);
    }

    internal static BinaryFunction GetLessOrEqualsDelegate(DataType leftType, DataType rightType)
    {
        var lessDelegate = GetLessDelegate(leftType, rightType);
        var equalsDelegate = GetEqualsDelegate(leftType, rightType);

        return (ref VariantValue left, ref VariantValue right) =>
        {
            if (left.IsNull || right.IsNull)
            {
                return Null;
            }
            var result = lessDelegate.Invoke(ref left, ref right).AsBooleanUnsafe
                || equalsDelegate.Invoke(ref left, ref right).AsBooleanUnsafe;
            return new VariantValue(result);
        };
    }
}
