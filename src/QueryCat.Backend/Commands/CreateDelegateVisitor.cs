using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Generate delegate for the specific node.
/// </summary>
internal class CreateDelegateVisitor : AstVisitor
{
    private readonly ExecutionThread _thread;

    protected Dictionary<int, IFuncUnit> NodeIdFuncMap { get; } = new(capacity: 32);

    /// <summary>
    /// AST traversal.
    /// </summary>
    protected AstTraversal AstTraversal { get; }

    protected ExecutionThread ExecutionThread => _thread;

    public CreateDelegateVisitor(ExecutionThread thread)
    {
        _thread = thread;
        AstTraversal = new AstTraversal(this);
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        AstTraversal.PostOrder(node);
    }

    public virtual IFuncUnit RunAndReturn(IAstNode node)
    {
        NodeIdFuncMap.Clear();
        Run(node);
        return NodeIdFuncMap[node.Id];
    }

    #region General

    /// <inheritdoc />
    public override void Visit(BetweenExpressionNode node)
    {
        var valueAction = NodeIdFuncMap[node.Expression.Id];
        var leftAction = NodeIdFuncMap[node.Left.Id];
        var rightAction = NodeIdFuncMap[node.Right.Id];

        VariantValue Func()
        {
            var value = valueAction.Invoke();
            var leftValue = leftAction.Invoke();
            var rightValue = rightAction.Invoke();
            var result = VariantValue.Between(ref value, ref leftValue, ref rightValue, out ErrorCode code);
            ApplyStatistic(code);
            var boolResult = result.AsBoolean;
            return node.IsNot ? new VariantValue(!boolResult) : new VariantValue(boolResult);
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func);
    }

    /// <inheritdoc />
    public override void Visit(BinaryOperationExpressionNode node)
    {
        var leftAction = NodeIdFuncMap[node.Left.Id];
        var rightAction = NodeIdFuncMap[node.Right.Id];
        var action = VariantValue.GetOperationDelegate(node.Operation,
            node.Left.GetDataType(), node.Right.GetDataType());

        VariantValue Func()
        {
            var leftValue = leftAction.Invoke();
            var rightValue = rightAction.Invoke();
            var result = action.Invoke(ref leftValue, ref rightValue);
            return result;
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func);
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        throw new CannotFindIdentifierException(node.Name);
    }

    /// <inheritdoc />
    public override void Visit(InOperationExpressionNode node)
    {
        var actions = node.InExpressionValues.Values.Select(v => NodeIdFuncMap[v.Id]).ToArray();
        var valueAction = NodeIdFuncMap[node.Expression.Id];

        VariantValue Func()
        {
            var leftValue = valueAction.Invoke();
            for (int i = 0; i < actions.Length; i++)
            {
                var rightValue = actions[i].Invoke();
                var isEqual = VariantValue.Equals(ref leftValue, ref rightValue, out ErrorCode code);
                ApplyStatistic(code);
                if (code != ErrorCode.OK)
                {
                    return VariantValue.Null;
                }
                if (!node.IsNot && isEqual.AsBoolean)
                {
                    return isEqual;
                }
            }
            return new VariantValue(node.IsNot);
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func);
    }

    /// <inheritdoc />
    public override void Visit(LiteralNode node)
    {
        NodeIdFuncMap[node.Id] = new FuncUnitStatic(node.Value);
    }

    /// <inheritdoc />
    public override void Visit(ProgramNode node)
    {
        var actions = node.Statements.Select(n => NodeIdFuncMap[n.Id]).ToArray();
        NodeIdFuncMap[node.Id] = new FuncUnitMultiDelegate(actions);
    }

    /// <inheritdoc />
    public override void Visit(UnaryOperationExpressionNode node)
    {
        var action = NodeIdFuncMap[node.Right.Id];

        switch (node.Operation)
        {
            case VariantValue.Operation.Subtract:
                NodeIdFuncMap[node.Id] = new FuncUnitDelegate(() =>
                {
                    var value = action.Invoke();
                    var result = VariantValue.Negation(ref value, out ErrorCode code);
                    ApplyStatistic(code);
                    return result;
                });
                break;
            case VariantValue.Operation.Not:
                NodeIdFuncMap[node.Id] = new FuncUnitDelegate(() =>
                {
                    var value = action.Invoke();
                    var result = VariantValue.Not(ref value, out ErrorCode code);
                    ApplyStatistic(code);
                    return result;
                });
                break;
            case VariantValue.Operation.IsNull:
                NodeIdFuncMap[node.Id] = new FuncUnitDelegate(() =>
                {
                    var value = action.Invoke();
                    return new VariantValue(value.IsNull);
                });
                break;
            case VariantValue.Operation.IsNotNull:
                NodeIdFuncMap[node.Id] = new FuncUnitDelegate(() =>
                {
                    var value = action.Invoke();
                    return new VariantValue(!value.IsNull);
                });
                break;
            default:
                throw new QueryCatException(Resources.Errors.InvalidOperation);
        }
    }

    #endregion

    #region Special functions

    /// <inheritdoc />
    public override void Visit(CastFunctionNode node)
    {
        var expressionAction = NodeIdFuncMap[node.ExpressionNode.Id];

        VariantValue Func()
        {
            var expressionValue = expressionAction.Invoke();
            if (expressionValue.Cast(node.TargetTypeNode.Type, out VariantValue result))
            {
                return result;
            }
            ApplyStatistic(ErrorCode.CannotCast);
            return VariantValue.Null;
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func);
    }

    /// <inheritdoc />
    public override void Visit(CoalesceFunctionNode node)
    {
        var expressionActions = node.Expressions.Select(e => NodeIdFuncMap[e.Id]).ToArray();

        VariantValue Func()
        {
            foreach (var expressionAction in expressionActions)
            {
                var value = expressionAction.Invoke();
                if (!value.IsNull)
                {
                    return value;
                }
            }
            return VariantValue.Null;
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func);
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override void Visit(FunctionCallArgumentNode node)
    {
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionValue.Id];
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        var function = node.GetAttribute<Function>(AstAttributeKeys.FunctionKey);
        if (function == null)
        {
            throw new InvalidOperationException("Function not set.");
        }

        var argsDelegatesList = new List<IFuncUnit>(function.Arguments.Length + 1);
        for (int i = 0; i < function.Arguments.Length; i++)
        {
            var argument = function.Arguments[i];

            // Try to set named first.
            var positionalArgumentValue = node.Arguments
                .FirstOrDefault(a => !a.IsPositional && a.Key!.Equals(argument.Name));
            if (positionalArgumentValue != null)
            {
                argsDelegatesList.Add(NodeIdFuncMap[positionalArgumentValue.Id]);
                continue;
            }

            // Try positional.
            if (node.Arguments.Count >= i + 1 && node.Arguments[i].IsPositional)
            {
                int argPosition = i;
                argsDelegatesList.Add(NodeIdFuncMap[node.Arguments[argPosition].Id]);
                continue;
            }

            // Try optional.
            if (function.Arguments[i].HasDefaultValue)
            {
                int argPosition = i;
                argsDelegatesList.Add(new FuncUnitStatic(function.Arguments[argPosition].DefaultValue));
                continue;
            }

            throw new InvalidFunctionArgumentException(
                string.Format(Resources.Errors.CannotSetArgument, argument.Name));
        }

        // Fill variadic.
        var hasVariadicArgument = function.Arguments.Length > 0 && function.Arguments[^1].IsVariadic;
        if (hasVariadicArgument)
        {
            for (int i = argsDelegatesList.Count; i < node.Arguments.Count; i++)
            {
                argsDelegatesList.Add(NodeIdFuncMap[node.Arguments[i].Id]);
            }
        }

        var argsDelegates = argsDelegatesList.ToArray();
        var callInfo = new FunctionCallInfo(argsDelegates);
        callInfo.FunctionsManager = _thread.FunctionsManager;
        node.SetAttribute(AstAttributeKeys.ArgumentsKey, callInfo);
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(() =>
        {
            callInfo.Reset();
            callInfo.InvokePushArgs();
            return function.Delegate(callInfo);
        });
    }

    #endregion

    protected void ApplyStatistic(ErrorCode code)
    {
        if (code != ErrorCode.OK)
        {
            _thread.Statistic.IncrementErrorsCount(code);
        }
    }
}
