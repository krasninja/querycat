// ReSharper disable CompareOfFloatsByEqualityOperator

using System.Text.RegularExpressions;

#pragma warning disable CS8509
namespace QueryCat.Backend.Core.Types;

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
        Similar,
        NotSimilar,
        In,

        // Logical.
        And = 200,
        Or,
        Not,

        // Other.
        Concat = 300
    }

    public delegate VariantValue OperationBinaryDelegate(in VariantValue left, in VariantValue right,
        out ErrorCode errorCode);

    internal static BinaryFunction GetBinaryFunction(OperationBinaryDelegate @delegate)
    {
        return (in VariantValue left, in VariantValue right)
            => @delegate.Invoke(in left, in right, out _);
    }

    internal static UnaryFunction GetOperationDelegate(Operation operation, DataType leftType)
        => operation switch
        {
            Operation.Not => GetNotDelegate(leftType),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), Resources.Errors.InvalidOperation),
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
            Operation.Similar => Similar,
            Operation.NotSimilar => NotSimilar,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), Resources.Errors.InvalidOperation),
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

    internal delegate VariantValue OperationTernaryDelegate(in VariantValue left, in VariantValue right,
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
        Operation.Like, Operation.NotLike,
        Operation.Similar, Operation.NotSimilar,
    };

    internal static Operation[] LogicalOperations { get; } =
    {
        Operation.And, Operation.Or, Operation.Not,
    };

    internal static Operation[] MiscOperations { get; } =
    {
        Operation.Concat,
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
            && (left == DataType.String || right == DataType.String))
        {
            return DataType.String;
        }

        if (MiscOperations.Contains(operation)
            && left == DataType.Blob && right == DataType.Blob)
        {
            return DataType.Blob;
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

        // The expression with dynamic type leads to dynamic.
        if (left == DataType.Dynamic || right == DataType.Dynamic)
        {
            return DataType.Dynamic;
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
            if (canConvert)
            {
                return target;
            }
        }

        return DataType.Void;
    }

    private static readonly DataType[] _stringCompatibleTypes =
    [
        DataType.Boolean, DataType.Timestamp, DataType.Dynamic, DataType.Object
    ];

    private static bool GetTargetType(in DataType left, in DataType right, out DataType target)
    {
        if (left == DataType.Integer
            && (right == DataType.Float || right == DataType.Numeric))
        {
            target = right;
            return true;
        }
        if ((DataTypeUtils.IsNumeric(left) && right == DataType.String)
            || (DataTypeUtils.IsNumeric(right) && left == DataType.String))
        {
            target = DataType.String;
            return true;
        }
        if ((_stringCompatibleTypes.Contains(left) && right == DataType.String)
            || (_stringCompatibleTypes.Contains(right) && left == DataType.String))
        {
            target = DataType.String;
            return true;
        }
        target = DataType.Void;
        return false;
    }

    #region Algebraic operations

    public delegate VariantValue UnaryFunction(in VariantValue left);

    public static VariantValue UnaryNullDelegate(in VariantValue left) => Null;

    public delegate VariantValue BinaryFunction(in VariantValue left, in VariantValue right);

    private static VariantValue BinaryNullDelegate(in VariantValue left, in VariantValue right)
        => Null;

    public static VariantValue Negation(in VariantValue left, out ErrorCode errorCode)
    {
        var function = GetNegationDelegate(left.Type);
        if (function == UnaryNullDelegate)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return function.Invoke(in left);
    }

    public static VariantValue Modulo(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var result = left.Type switch
        {
            DataType.Integer => right.Type switch
            {
                DataType.Integer => new VariantValue(left.AsIntegerUnsafe % right.AsIntegerUnsafe),
                DataType.Float => new VariantValue(left.AsIntegerUnsafe % right.AsFloatUnsafe),
                DataType.Numeric => new VariantValue(left.AsIntegerUnsafe % right.AsNumericUnsafe),
                _ => Null,
            },
            DataType.Float => right.Type switch
            {
                DataType.Integer => new VariantValue(left.AsFloatUnsafe % right.AsIntegerUnsafe),
                DataType.Float => new VariantValue(left.AsFloatUnsafe % right.AsFloatUnsafe),
                _ => Null,
            },
            DataType.Numeric => right.Type switch
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

    public static VariantValue LeftShift(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var result = left.Type switch
        {
            DataType.Integer => right.Type switch
            {
                DataType.Integer => new VariantValue((int)left.AsIntegerUnsafe << (int)right.AsIntegerUnsafe),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    public static VariantValue RightShift(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var result = left.Type switch
        {
            DataType.Integer => right.Type switch
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

    public static VariantValue Between(
        in VariantValue value,
        in VariantValue left,
        in VariantValue right,
        out ErrorCode errorCode)
    {
        if (value.IsNull || left.IsNull || right.IsNull)
        {
            errorCode = ErrorCode.OK;
            return Null;
        }

        var leftCondition = GreaterOrEquals(in value, in left, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }
        var rightCondition = LessOrEquals(in value, in right, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }

        errorCode = ErrorCode.OK;
        return new VariantValue(leftCondition.AsBoolean && rightCondition.AsBoolean);
    }

    public static VariantValue BetweenAnd(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        throw new InvalidOperationException(Resources.Errors.AndShouldNotBeWithinBetweenOperation);
    }

    public static VariantValue Like(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var pattern = right.AsString;
        var str = left.AsString;

        errorCode = ErrorCode.OK;
        return new VariantValue(StringLikeEquals.Equals(pattern, str));
    }

    public static VariantValue NotLike(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var likeResult = Like(in left, in right, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }

        return likeResult.AsBoolean ? new VariantValue(false) : new VariantValue(true);
    }

    public static VariantValue Similar(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var pattern = right.AsString;
        var str = left.AsString;

        errorCode = ErrorCode.OK;
        var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        return new VariantValue(regex.IsMatch(str));
    }

    public static VariantValue NotSimilar(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        var likeResult = Similar(in left, in right, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }

        return likeResult.AsBoolean ? new VariantValue(false) : new VariantValue(true);
    }

    #endregion

    #region Concatenation operator

    public static VariantValue Concat(in VariantValue left, in VariantValue right, out ErrorCode errorCode)
    {
        VariantValue CreateCombinedBlob(IBlobData leftBlob, IBlobData rightBlob)
        {
            return new VariantValue(new StreamBlobData(
                () => new MultiStream(
                    leftBlob.GetStream(), rightBlob.GetStream()), leftBlob.ContentType
                )
            );
        }

        var result = left.Type switch
        {
            DataType.Integer => right.Type switch
            {
                DataType.String => new VariantValue(string.Concat(left.AsString, right.AsStringUnsafe)),
            },
            DataType.Float => right.Type switch
            {
                DataType.String => new VariantValue(string.Concat(left.AsString, right.AsStringUnsafe)),
            },
            DataType.Numeric => right.Type switch
            {
                DataType.String => new VariantValue(string.Concat(left.AsString, right.AsStringUnsafe)),
            },
            DataType.String => right.Type switch
            {
                DataType.String => new VariantValue(string.Concat(left.AsStringUnsafe, right.AsStringUnsafe)),
                DataType.Integer => new VariantValue(string.Concat(left.AsStringUnsafe, right.AsString)),
                DataType.Float => new VariantValue(string.Concat(left.AsStringUnsafe, right.AsString)),
                DataType.Numeric => new VariantValue(string.Concat(left.AsStringUnsafe, right.AsString)),
                _ => Null,
            },
            DataType.Blob => right.Type switch
            {
                DataType.Blob => CreateCombinedBlob(left.AsBlobUnsafe, right.AsBlobUnsafe),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    #endregion
}
