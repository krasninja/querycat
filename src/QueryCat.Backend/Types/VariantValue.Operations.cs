// ReSharper disable CompareOfFloatsByEqualityOperator

using QueryCat.Backend.Utils;

#pragma warning disable CS8509
namespace QueryCat.Backend.Types;

public readonly partial struct VariantValue
{
    /// <summary>
    /// Math operations.
    /// </summary>
    public enum Operation
    {
        // Algebraic.
        Add = 0,
        Subtract,
        Multiple,
        Divide,
        Modulo,
        LeftShift,
        RightShift,

        // Comparision.
        Equals = 100,
        NotEquals,
        Greater,
        GreaterOrEquals,
        Less,
        LessOrEquals,
        Between,
        BetweenAnd,
        IsNull,
        IsNotNull,
        Like,
        NotLike,
        In,

        // Logical.
        And = 200,
        Or,
        Not,

        // Other.
        Concat = 300
    }

    internal delegate VariantValue OperationBinaryDelegate(ref VariantValue left, ref VariantValue right,
        out ErrorCode errorCode);

    internal static BinaryFunction GetBinaryFunction(OperationBinaryDelegate @delegate)
    {
        return (ref VariantValue left, ref VariantValue right)
            => @delegate.Invoke(ref left, ref right, out _);
    }

    internal static UnaryFunction GetOperationDelegate(Operation operation, DataType leftType)
        => operation switch
        {
            Operation.Not => GetNotDelegate(leftType),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), "Invalid operation."),
        };

    internal static OperationBinaryDelegate GetOperationDelegate(Operation operation)
        => operation switch
        {
            Operation.Add => Add,
            Operation.Subtract => Subtract,
            Operation.Multiple => Mul,
            Operation.Divide => Div,
            Operation.Modulo => Modulo,
            Operation.LeftShift => LeftShift,
            Operation.RightShift => RightShift,
            Operation.Equals => Equals,
            Operation.NotEquals => NotEquals,
            Operation.Greater => Greater,
            Operation.GreaterOrEquals => GreaterOrEquals,
            Operation.Less => Less,
            Operation.LessOrEquals => LessOrEquals,
            Operation.Concat => Concat,
            Operation.BetweenAnd => BetweenAnd,
            Operation.Like => Like,
            Operation.NotLike => NotLike,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), "Invalid operation."),
        };

    internal static BinaryFunction GetOperationDelegate(Operation operation, DataType leftType, DataType rightType)
        => operation switch
        {
            Operation.Add => GetAddDelegate(leftType, rightType),
            Operation.Subtract => GetSubtractDelegate(leftType, rightType),
            Operation.Multiple => GetMulDelegate(leftType, rightType),
            Operation.Divide => GetDivDelegate(leftType, rightType),
            Operation.Equals => GetEqualsDelegate(leftType, rightType),
            Operation.NotEquals => GetNotEqualsDelegate(leftType, rightType),
            Operation.Greater => GetGreaterDelegate(leftType, rightType),
            Operation.GreaterOrEquals => GetGreaterOrEqualsDelegate(leftType, rightType),
            Operation.Less => GetLessDelegate(leftType, rightType),
            Operation.LessOrEquals => GetLessOrEqualsDelegate(leftType, rightType),
            Operation.And => GetAndDelegate(leftType, rightType),
            Operation.Or => GetOrDelegate(leftType, rightType),
            _ => GetBinaryFunction(GetOperationDelegate(operation)),
        };

    internal delegate VariantValue OperationTernaryDelegate(ref VariantValue left, ref VariantValue right,
        out ErrorCode errorCode);

    internal static Operation[] AlgebraicOperations { get; } =
    {
        Operation.Add,
        Operation.Subtract,
        Operation.Multiple,
        Operation.Divide,
        Operation.Modulo,
        Operation.LeftShift,
        Operation.RightShift,
    };

    internal static Operation[] ComparisionOperations { get; } =
    {
        Operation.Equals, Operation.NotEquals,
        Operation.Greater, Operation.GreaterOrEquals,
        Operation.Less, Operation.LessOrEquals,
        Operation.Between, Operation.BetweenAnd,
        Operation.Like, Operation.NotLike
    };

    internal static Operation[] LogicalOperations { get; } =
    {
        Operation.And, Operation.Or, Operation.Not
    };

    internal static Operation[] MiscOperations { get; } =
    {
        Operation.Concat
    };

    /// <summary>
    /// Get the target result type of binary operation of two values.
    /// If the result type cannot be determined the Void type will be returned.
    /// </summary>
    /// <param name="left">Left value type.</param>
    /// <param name="right">Right value type.</param>
    /// <param name="operation">Binary operation.</param>
    /// <returns>The target type.</returns>
    internal static DataType GetResultType(in DataType left, in DataType right,
        in Operation operation)
    {
        if (ComparisionOperations.Contains(operation))
        {
            return DataType.Boolean;
        }

        if (LogicalOperations.Contains(operation)
            && left == DataType.Boolean && right == DataType.Boolean)
        {
            return DataType.Boolean;
        }

        if (MiscOperations.Contains(operation)
            && left == DataType.String && right == DataType.String)
        {
            return DataType.String;
        }

        if (operation == Operation.Add || operation == Operation.Subtract)
        {
            if ((left == DataType.Timestamp && right == DataType.Interval)
                || (left == DataType.Interval && right == DataType.Timestamp))
            {
                return DataType.Timestamp;
            }
            if (left == DataType.Interval && right == DataType.Interval)
            {
                return DataType.Interval;
            }
        }

        if (operation == Operation.Multiple)
        {
            if ((left == DataType.Integer && right == DataType.Interval)
                || (left == DataType.Interval && right == DataType.Integer))
            {
                return DataType.Interval;
            }
        }

        if (operation == Operation.Divide && left == DataType.Interval && right == DataType.Integer)
        {
            return DataType.Interval;
        }

        if (left == DataType.Null && right != DataType.Null)
        {
            return right;
        }
        if (left != DataType.Null && right == DataType.Null)
        {
            return left;
        }

        if (AlgebraicOperations.Contains(operation))
        {
            if (left == right)
            {
                return right;
            }

            var canConvert = GetTargetType(left, right, out var target);
            if (!canConvert)
            {
                canConvert = GetTargetType(right, left, out target);
            }
            return target;
        }

        return DataType.Void;
    }

    private static bool GetTargetType(in DataType left, in DataType right, out DataType target)
    {
        if (left == DataType.Integer
            && (right == DataType.Float || right == DataType.Numeric))
        {
            target = right;
            return true;
        }
        target = DataType.Void;
        return false;
    }

    #region Algebraic operations

    public delegate VariantValue UnaryFunction(ref VariantValue left);

    public static VariantValue UnaryNullDelegate(ref VariantValue left) => Null;

    public delegate VariantValue BinaryFunction(ref VariantValue left, ref VariantValue right);

    private static VariantValue BinaryNullDelegate(ref VariantValue left, ref VariantValue right)
        => Null;

    public static VariantValue Negation(ref VariantValue left, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();

        var function = GetNegationDelegate(leftType);
        if (function == UnaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(ref left);
    }

    public static VariantValue Modulo(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsIntegerUnsafe % right.AsIntegerUnsafe),
                DataType.Float => new VariantValue(left.AsIntegerUnsafe % right.AsFloatUnsafe),
                DataType.Numeric => new VariantValue(left.AsIntegerUnsafe % right.AsNumericUnsafe),
                _ => Null,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsFloatUnsafe % right.AsIntegerUnsafe),
                DataType.Float => new VariantValue(left.AsFloatUnsafe % right.AsFloatUnsafe),
                _ => Null,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsNumericUnsafe % right.AsIntegerUnsafe),
                DataType.Numeric => new VariantValue(left.AsNumericUnsafe % right.AsNumericUnsafe),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    public static VariantValue LeftShift(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => new VariantValue((int)left.AsIntegerUnsafe << (int)right.AsIntegerUnsafe),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    public static VariantValue RightShift(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => new VariantValue((int)left.AsIntegerUnsafe >> (int)right.AsIntegerUnsafe),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    #endregion

    #region Comparision operations

    public static VariantValue Between(ref VariantValue value,
        ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        if (value.IsNull || left.IsNull || right.IsNull)
        {
            errorCode = ErrorCode.OK;
            return Null;
        }

        var leftCondition = GreaterOrEquals(ref value, ref left, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }
        var rightCondition = LessOrEquals(ref value, ref right, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }

        errorCode = ErrorCode.OK;
        return new VariantValue(leftCondition.AsBoolean && rightCondition.AsBoolean);
    }

    public static VariantValue BetweenAnd(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        throw new InvalidOperationException("AND operation should not be evaluated within BETWEEN expression!");
    }

    public static VariantValue Like(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var pattern = right.AsString;
        var str = left.AsString;

        errorCode = ErrorCode.OK;
        return new VariantValue(StringUtils.MatchesToLikePattern(pattern, str));
    }

    public static VariantValue NotLike(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var likeResult = Like(ref left, ref right, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }

        return likeResult.AsBoolean ? new VariantValue(false) : new VariantValue(true);
    }

    #endregion

    #region Concatenation operator

    public static VariantValue Concat(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        var result = leftType switch
        {
            DataType.String => rightType switch
            {
                DataType.String => new VariantValue(string.Concat(left.AsStringUnsafe, right.AsStringUnsafe)),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    #endregion
}
