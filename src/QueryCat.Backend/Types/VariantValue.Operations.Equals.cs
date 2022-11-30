// ReSharper disable CompareOfFloatsByEqualityOperator
namespace QueryCat.Backend.Types;

public partial struct VariantValue
{
    internal static VariantValue Equals(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var function = GetEqualsDelegate(leftType, rightType);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(ref left, ref right);
    }

    internal static BinaryFunction GetEqualsDelegate(DataType type) => GetEqualsDelegate(type, type);

    internal static BinaryFunction GetEqualsDelegate(DataType leftType, DataType rightType)
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
                    return new VariantValue(left.AsIntegerUnsafe == right.AsIntegerUnsafe);
                },
                DataType.Float => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe == right.AsFloatUnsafe);
                },
                DataType.Numeric => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe == right.AsNumericUnsafe);
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
                    return new VariantValue(left.AsFloatUnsafe == right.AsIntegerUnsafe);
                },
                DataType.Float => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsFloatUnsafe == right.AsFloatUnsafe);
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
                    return new VariantValue(left.AsNumericUnsafe == right.AsIntegerUnsafe);
                },
                DataType.Numeric => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsNumericUnsafe == right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Boolean => rightType switch
            {
                DataType.Boolean => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsBooleanUnsafe == right.AsBooleanUnsafe);
                },
                DataType.String => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsBooleanUnsafe == right.AsBoolean);
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
                    return new VariantValue(left.AsTimestampUnsafe == right.AsTimestampUnsafe);
                },
                DataType.String => (ref VariantValue left, ref VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsTimestampUnsafe == right.AsTimestamp);
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
                    return new VariantValue(left.AsIntervalUnsafe == right.AsIntervalUnsafe);
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
                    return new VariantValue(left.AsStringUnsafe == right.AsStringUnsafe);
                },
                DataType.Boolean or DataType.Integer or DataType.Integer
                    => (ref VariantValue left, ref VariantValue right) =>
                    {
                        if (left.IsNull || right.IsNull)
                        {
                            return Null;
                        }
                        return new VariantValue(left.AsStringUnsafe == right.AsString);
                    },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }

    internal static VariantValue NotEquals(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var result = Equals(ref left, ref right, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }
        return Negation(ref result, out errorCode);
    }

    internal static BinaryFunction GetNotEqualsDelegate(DataType leftType, DataType rightType)
    {
        var equalsDelegate = GetEqualsDelegate(leftType, rightType);
        return (ref VariantValue left, ref VariantValue right) =>
        {
            if (left.IsNull || right.IsNull)
            {
                return Null;
            }
            return new VariantValue(!equalsDelegate.Invoke(ref left, ref right).AsBooleanUnsafe);
        };
    }
}
