namespace QueryCat.Backend.Core.Types;

public partial struct VariantValue
{
    internal static VariantValue Less(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var function = GetLessDelegate(left.Type, right.Type);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(in left, in right);
    }

    internal static BinaryFunction GetLessDelegate(DataType leftType, DataType rightType)
    {
        return leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe < right.AsIntegerUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe < right.AsFloatUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe < right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe < right.AsIntegerUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe < right.AsFloatUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue((decimal)left.AsFloatUnsafe < right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe < right.AsIntegerUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe < right.AsNumericUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe < (decimal)right.AsFloatUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Boolean => rightType switch
            {
                DataType.Boolean or DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe < right.AsIntegerUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.String => rightType switch
            {
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(string.CompareOrdinal(left.AsStringUnsafe, right.AsStringUnsafe) < 0);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Timestamp => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsTimestampUnsafe < right.AsTimestampUnsafe);
                },
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsTimestampUnsafe < right.AsTimestamp);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Interval => rightType switch
            {
                DataType.Interval => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntervalUnsafe < right.AsIntervalUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }

    internal static VariantValue LessOrEquals(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var function = GetLessOrEqualsDelegate(left.Type, right.Type);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(in left, in right);
    }

    internal static BinaryFunction GetLessOrEqualsDelegate(DataType leftType, DataType rightType)
    {
        var lessDelegate = GetLessDelegate(leftType, rightType);
        var equalsDelegate = GetEqualsDelegate(leftType, rightType);

        return (in VariantValue left, in VariantValue right) =>
        {
            var result = lessDelegate.Invoke(in left, in right).AsBooleanUnsafe
                || equalsDelegate.Invoke(in left, in right).AsBooleanUnsafe;
            return new VariantValue(result);
        };
    }
}
