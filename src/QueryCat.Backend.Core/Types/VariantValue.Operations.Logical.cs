namespace QueryCat.Backend.Core.Types;

public partial struct VariantValue
{
    internal static UnaryFunction GetNotDelegate(DataType leftType)
    {
        if (!(leftType == DataType.Boolean || leftType == DataType.Null))
        {
            throw new SemanticException(string.Format(Resources.Errors.CannotGetOrDelegate, leftType));
        }

        return (in VariantValue left) =>
        {
            if (left.IsNull)
            {
                return Null;
            }
            return new VariantValue(!left.AsBooleanUnsafe);
        };
    }

    internal static BinaryFunction GetAndDelegate(DataType leftType, DataType rightType)
    {
        if (!((leftType == DataType.Boolean || leftType == DataType.Null)
            && (rightType == DataType.Boolean || rightType == DataType.Null)))
        {
            throw new SemanticException(string.Format(Resources.Errors.CannotGetAndDelegate, leftType, rightType));
        }

        return (in VariantValue left, in VariantValue right) =>
        {
            if ((left.IsNull && right.AsBooleanUnsafe)
                || (left.AsBooleanUnsafe && right.IsNull))
            {
                return Null;
            }
            if ((left.IsNull && !right.AsBooleanUnsafe)
                || (!left.AsBooleanUnsafe && right.IsNull))
            {
                return FalseValue;
            }
            return new VariantValue(left.AsBooleanUnsafe && right.AsBooleanUnsafe);
        };
    }

    internal static BinaryFunction GetOrDelegate(DataType leftType, DataType rightType)
    {
        if (!((leftType == DataType.Boolean || leftType == DataType.Null)
            && (rightType == DataType.Boolean || rightType == DataType.Null)))
        {
            throw new SemanticException(string.Format(Resources.Errors.CannotGetOrDelegate, leftType, rightType));
        }

        return (in VariantValue left, in VariantValue right) =>
        {
            if ((left.IsNull && right.AsBooleanUnsafe)
                || (left.AsBooleanUnsafe && right.IsNull))
            {
                return TrueValue;
            }
            if ((left.IsNull && !right.AsBooleanUnsafe)
                || (!left.AsBooleanUnsafe && right.IsNull))
            {
                return Null;
            }
            return new VariantValue(left.AsBooleanUnsafe || right.AsBooleanUnsafe);
        };
    }
}
