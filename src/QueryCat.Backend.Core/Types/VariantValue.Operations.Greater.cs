namespace QueryCat.Backend.Core.Types;

public partial struct VariantValue
{
    internal static VariantValue Greater(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var function = GetGreaterDelegate(left.Type, right.Type);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(in left, in right);
    }

    internal static BinaryFunction GetGreaterDelegate(DataType leftType, DataType rightType)
    {
        return leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe > right.AsIntegerUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe > right.AsFloatUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe > right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe > right.AsFloatUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe > right.AsFloatUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe > right.AsIntegerUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe > right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Boolean => rightType switch
            {
                DataType.Boolean or DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe > right.AsIntegerUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.String => rightType switch
            {
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(string.CompareOrdinal(left.AsStringUnsafe, right.AsStringUnsafe) > 0);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Timestamp => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsTimestampUnsafe > right.AsTimestampUnsafe);
                },
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsTimestampUnsafe > right.AsTimestamp);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Interval => rightType switch
            {
                DataType.Interval => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntervalUnsafe > right.AsIntervalUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }

    internal static VariantValue GreaterOrEquals(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var function = GetGreaterOrEqualsDelegate(left.Type, right.Type);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(in left, in right);
    }

    internal static BinaryFunction GetGreaterOrEqualsDelegate(DataType leftType, DataType rightType)
    {
        var greaterDelegate = GetGreaterDelegate(leftType, rightType);
        var equalsDelegate = GetEqualsDelegate(leftType, rightType);

        return (in VariantValue left, in VariantValue right) =>
        {
            var result = greaterDelegate.Invoke(in left, in right).AsBooleanUnsafe
                || equalsDelegate.Invoke(in left, in right).AsBooleanUnsafe;
            return new VariantValue(result);
        };
    }
}
