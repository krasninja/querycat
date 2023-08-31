using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Core.Types;

public readonly partial struct VariantValue
{
    internal static VariantValue Mul(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var function = GetMulDelegate(leftType, rightType);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(in left, in right);
    }

    internal static BinaryFunction GetMulDelegate(DataType leftType, DataType rightType)
    {
        return leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe * right.AsIntegerUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe * right.AsFloatUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe * right.AsNumericUnsafe);
                },
                DataType.Interval => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntegerUnsafe * right.AsIntervalUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsFloatUnsafe * right.AsIntegerUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsFloatUnsafe * right.AsFloatUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsNumericUnsafe * right.AsIntegerUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsNumericUnsafe * right.AsNumericUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Interval => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(left.AsIntervalUnsafe * right.AsIntegerUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }
}
