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

    internal static OperationBinaryDelegate GetOperationDelegate(Operation operation)
        => operation switch
        {
            Operation.Add => Add,
            Operation.Subtract => Subtract,
            Operation.Multiple => Mul,
            Operation.Divide => Div,
            Operation.Modulo => Modulo,
            Operation.Equals => Equals,
            Operation.NotEquals => NotEquals,
            Operation.Greater => Greater,
            Operation.GreaterOrEquals => GreaterOrEquals,
            Operation.Less => Less,
            Operation.LessOrEquals => LessOrEquals,
            Operation.And => And,
            Operation.Or => Or,
            Operation.Concat => Concat,
            Operation.BetweenAnd => BetweenAnd,
            Operation.Like => Like,
            Operation.NotLike => NotLike,
            _ => throw new ArgumentOutOfRangeException()
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
        if (left == DataType.Null || right == DataType.Null)
        {
            return DataType.Null;
        }

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
                || (right == DataType.Interval && left == DataType.Timestamp))
            {
                return DataType.Timestamp;
            }
            if (left == DataType.Interval && right == DataType.Interval)
            {
                return DataType.Interval;
            }
        }

        if (AlgebraicOperations.Contains(operation))
        {
            if (left == right)
            {
                return right;
            }

            bool canConvert = GetTargetType(left, right, out var target);
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

    public static VariantValue Negation(ref VariantValue left, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();

        VariantValue result = leftType switch
        {
            DataType.Integer => new VariantValue(-left.AsInteger),
            DataType.Float => new VariantValue(-left.AsFloat),
            DataType.Numeric => new VariantValue(-left.AsNumeric),
            DataType.Boolean => new VariantValue(!left.AsBoolean),
            DataType.Interval => new VariantValue(-left.AsInterval),
            _ => Null,
        };
        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    public static VariantValue Add(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        VariantValue result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsInteger + right.AsInteger),
                DataType.Float => new VariantValue(left.AsInteger + right.AsFloat),
                DataType.Numeric => new VariantValue(left.AsInteger + right.AsNumeric),
                _ => Null,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsFloat + right.AsInteger),
                DataType.Float => new VariantValue(left.AsFloat + right.AsFloat),
                _ => Null,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsNumeric + right.AsInteger),
                DataType.Numeric => new VariantValue(left.AsNumeric + right.AsNumeric),
                _ => Null,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Interval => new VariantValue(left.AsTimestamp + right.AsInterval),
                _ => Null,
            },
            DataType.Interval => rightType switch
            {
                DataType.Interval => new VariantValue(left.AsInterval + right.AsInterval),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    public static VariantValue Subtract(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var negativeRight = Negation(ref right, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }
        return Add(ref left, ref negativeRight, out errorCode);
    }

    public static VariantValue Mul(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        VariantValue result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsInteger * right.AsInteger),
                DataType.Float => new VariantValue(left.AsInteger * right.AsFloat),
                DataType.Numeric => new VariantValue(left.AsInteger * right.AsNumeric),
                _ => Null,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsFloat * right.AsInteger),
                DataType.Float => new VariantValue(left.AsFloat * right.AsFloat),
                _ => Null,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsNumeric * right.AsInteger),
                DataType.Numeric => new VariantValue(left.AsNumeric * right.AsNumeric),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    public static VariantValue Div(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        VariantValue result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsInteger / right.AsInteger),
                DataType.Float => new VariantValue(left.AsInteger / right.AsFloat),
                DataType.Numeric => new VariantValue(left.AsInteger / right.AsNumeric),
                _ => Null,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsFloat / right.AsInteger),
                DataType.Float => new VariantValue(left.AsFloat / right.AsFloat),
                _ => Null,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsNumeric / right.AsInteger),
                DataType.Numeric => new VariantValue(left.AsNumeric / right.AsNumeric),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    public static VariantValue Modulo(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        VariantValue result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsInteger % right.AsInteger),
                DataType.Float => new VariantValue(left.AsInteger % right.AsFloat),
                DataType.Numeric => new VariantValue(left.AsInteger % right.AsNumeric),
                _ => Null,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsFloat % right.AsInteger),
                DataType.Float => new VariantValue(left.AsFloat % right.AsFloat),
                _ => Null,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => new VariantValue(left.AsNumeric % right.AsInteger),
                DataType.Numeric => new VariantValue(left.AsNumeric % right.AsNumeric),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    #endregion

    #region Comparision operations

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

    public static VariantValue NotEquals(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var result = Equals(ref left, ref right, out errorCode);
        if (errorCode != ErrorCode.OK)
        {
            return Null;
        }
        return Negation(ref result, out errorCode);
    }

    public static VariantValue Greater(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        bool? result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => left.AsInteger > right.AsInteger,
                DataType.Float => left.AsInteger > right.AsFloat,
                DataType.Numeric => left.AsInteger > right.AsNumeric,
                _ => null,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => left.AsFloat > right.AsInteger,
                DataType.Float => left.AsFloat > right.AsFloat,
                _ => null,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => left.AsNumeric > right.AsInteger,
                DataType.Numeric => left.AsNumeric > right.AsNumeric,
                _ => null,
            },
            DataType.Boolean => rightType switch
            {
                DataType.Boolean or DataType.Integer => left.AsInteger > right.AsInteger,
                _ => null,
            },
            DataType.String => rightType switch
            {
                DataType.String => string.CompareOrdinal(left.AsString, right.AsString) > 0,
                _ => null,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Timestamp or DataType.String => left.AsTimestamp > right.AsTimestamp,
                _ => null,
            },
            DataType.Interval => rightType switch
            {
                DataType.Interval => left.AsInterval > right.AsInterval,
                _ => null,
            },
            _ => null,
        };
        errorCode = result.HasValue ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result.HasValue
            ? new VariantValue(result.Value)
            : Null;
    }

    public static VariantValue GreaterOrEquals(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
        => new(
            Greater(ref left, ref right, out errorCode).AsBoolean
            || Equals(ref left, ref right, out errorCode).AsBoolean);

    public static VariantValue Less(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        bool? result = leftType switch
        {
            DataType.Integer => rightType switch
            {
                DataType.Integer => left.AsInteger < right.AsInteger,
                DataType.Float => left.AsInteger < right.AsFloat,
                DataType.Numeric => left.AsInteger < right.AsNumeric,
                _ => null,
            },
            DataType.Float => rightType switch
            {
                DataType.Integer => left.AsFloat < right.AsInteger,
                DataType.Float => left.AsFloat < right.AsFloat,
                _ => null,
            },
            DataType.Numeric => rightType switch
            {
                DataType.Integer => left.AsNumeric < right.AsInteger,
                DataType.Numeric => left.AsNumeric < right.AsNumeric,
                _ => null,
            },
            DataType.Boolean => rightType switch
            {
                DataType.Boolean or DataType.Integer => left.AsInteger < right.AsInteger,
                _ => null,
            },
            DataType.String => rightType switch
            {
                DataType.String => string.CompareOrdinal(left.AsString, right.AsString) < 0,
                _ => null,
            },
            DataType.Timestamp => rightType switch
            {
                DataType.Timestamp or DataType.String => left.AsTimestamp < right.AsTimestamp,
                _ => null,
            },
            DataType.Interval => rightType switch
            {
                DataType.Interval => left.AsInterval < right.AsInterval,
                _ => null,
            },
            _ => null,
        };
        errorCode = result.HasValue ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result.HasValue
            ? new VariantValue(result.Value)
            : Null;
    }

    public static VariantValue LessOrEquals(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
        => new(
            Less(ref left, ref right, out errorCode).AsBoolean
            || Equals(ref left, ref right, out errorCode).AsBoolean);

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

    #region Logical operations

    public static VariantValue And(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        if (left.IsNull)
        {
            errorCode = ErrorCode.OK;
            return Null;
        }
        var leftType = left.GetInternalType();
        if (leftType != DataType.Boolean)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }
        if (!left.AsBoolean)
        {
            errorCode = ErrorCode.OK;
            return left;
        }

        if (right.IsNull)
        {
            errorCode = ErrorCode.OK;
            return Null;
        }
        var rightType = right.GetInternalType();
        if (rightType != DataType.Boolean)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }
        if (!right.AsBoolean)
        {
            errorCode = ErrorCode.OK;
            return right;
        }

        errorCode = ErrorCode.OK;
        return new VariantValue(true);
    }

    public static VariantValue Or(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        if (left.IsNull)
        {
            errorCode = ErrorCode.OK;
            return Null;
        }
        var leftType = left.GetInternalType();
        if (leftType != DataType.Boolean)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }
        if (left.AsBoolean)
        {
            errorCode = ErrorCode.OK;
            return left;
        }

        if (right.IsNull)
        {
            errorCode = ErrorCode.OK;
            return Null;
        }
        var rightType = right.GetInternalType();
        if (rightType != DataType.Boolean)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }
        if (right.AsBoolean)
        {
            errorCode = ErrorCode.OK;
            return right;
        }

        errorCode = ErrorCode.OK;
        return new VariantValue(false);
    }

    public static VariantValue Not(ref VariantValue right, out ErrorCode errorCode)
    {
        if (right.IsNull)
        {
            errorCode = ErrorCode.OK;
            return Null;
        }
        var rightType = right.GetInternalType();
        if (rightType != DataType.Boolean)
        {
            errorCode = ErrorCode.CannotApplyOperator;
            return Null;
        }

        errorCode = ErrorCode.OK;
        return new VariantValue(!right.AsBoolean);
    }

    #endregion

    #region Concatenation operator

    public static VariantValue Concat(ref VariantValue left, ref VariantValue right, out ErrorCode errorCode)
    {
        var leftType = left.GetInternalType();
        var rightType = right.GetInternalType();

        VariantValue result = leftType switch
        {
            DataType.String => rightType switch
            {
                DataType.String => new VariantValue(string.Concat(left.AsString, right.AsString)),
                _ => Null,
            },
            _ => Null,
        };

        errorCode = !result.IsNull ? ErrorCode.OK : ErrorCode.CannotApplyOperator;
        return result;
    }

    #endregion
}
