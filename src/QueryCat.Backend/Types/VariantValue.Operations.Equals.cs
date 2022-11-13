// ReSharper disable CompareOfFloatsByEqualityOperator
namespace QueryCat.Backend.Types;

public partial struct VariantValue
{
    public static VariantValue Equals(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        bool? result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => left.AsInteger == right.AsInteger,
                DataType.Float => left.AsInteger == right.AsFloat,
                DataType.Numeric => left.AsInteger == right.AsNumeric,
                _ => null,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => left.AsFloat == right.AsInteger,
                DataType.Float => left.AsFloat == right.AsFloat,
                _ => null,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => left.AsNumeric == right.AsInteger,
                DataType.Numeric => left.AsNumeric == right.AsNumeric,
                _ => null,
            },
            DataType.Boolean => rightType switch
            {
                DataType.Boolean or DataType.String => left.AsInteger == right.AsInteger,
                _ => null,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Timestamp or DataType.String => left.AsTimestamp == right.AsTimestamp,
                _ => null,
            },
            DataType.Interval => rightType switch
            {
                DataType.Interval => left.AsInterval == right.AsInterval,
                _ => null,
            },
            DataType.String => rightType switch
            {
                DataType.String or DataType.Boolean or DataType.Integer or DataType.Integer
                    => left.AsString == right.AsString,
                _ => null,
            },
            _ => null,
        };
        errorCode = result.HasValue ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result.HasValue
            ? new VariantValue(result.Value)
            : Null;
    }

    public static BinaryFunction GetEqualsDelegate(DataType leftType, DataType rightType)
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

    public static VariantValue NotEquals(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var result = Equals(ref left, ref right, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }
        return Negation(ref result, out errorCode);
    }
}
