namespace QueryCat.Backend.Core.Types;

public partial struct VariantValue
{
    internal static VariantValue Add(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var function = GetAddDelegate(left.Type, right.Type);
        if (function == BinaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(in left, in right);
    }

    internal static BinaryFunction GetAddDelegate(DataType leftType, DataType rightType)
    {
        return leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe + right.AsIntegerUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe + right.AsFloatUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntegerUnsafe + right.AsNumericUnsafe);
                },
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(string.Concat(left.AsString, right.AsStringUnsafe));
                },
                _ => BinaryNullDelegate,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe + right.AsIntegerUnsafe);
                },
                DataType.Float => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsFloatUnsafe + right.AsFloatUnsafe);
                },
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(string.Concat(left.AsString, right.AsStringUnsafe));
                },
                _ => BinaryNullDelegate,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe + right.AsIntegerUnsafe);
                },
                DataType.Numeric => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsNumericUnsafe + right.AsNumericUnsafe);
                },
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(string.Concat(left.AsString, right.AsStringUnsafe));
                },
                _ => BinaryNullDelegate,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Interval => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsTimestampUnsafe + right.AsIntervalUnsafe);
                },
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsTimestampUnsafe + right.AsStringUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.Interval => rightType switch
            {
                DataType.Interval => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntervalUnsafe + right.AsIntervalUnsafe);
                },
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(left.AsIntervalUnsafe + right.AsStringUnsafe);
                },
                _ => BinaryNullDelegate,
            },
            DataType.String => rightType switch
            {
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(string.Concat(left.AsStringUnsafe, right.AsStringUnsafe));
                },
                DataType.Integer or DataType.Float or DataType.Numeric or DataType.Boolean => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(string.Concat(left.AsStringUnsafe, right.AsString));
                },
                DataType.Dynamic or DataType.Object or DataType.Interval or DataType.Timestamp => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(string.Concat(left.AsStringUnsafe, right.AsString));
                },
                _ => BinaryNullDelegate,
            },
            DataType.Boolean => rightType switch
            {
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    return new VariantValue(string.Concat(left.AsString, right.AsStringUnsafe));
                },
                _ => BinaryNullDelegate,
            },
            DataType.Dynamic or DataType.Object => rightType switch
            {
                DataType.String => (in VariantValue left, in VariantValue right) =>
                {
                    if (left.IsNull || right.IsNull)
                    {
                        return Null;
                    }
                    return new VariantValue(string.Concat(left.AsObjectUnsafe?.ToString(), right.AsStringUnsafe));
                },
                _ => BinaryNullDelegate,
            },
            _ => BinaryNullDelegate,
        };
    }
}
