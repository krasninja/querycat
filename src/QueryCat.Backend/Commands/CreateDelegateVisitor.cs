using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
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

    protected Dictionary<int, VariantValueFunc> NodeIdFuncMap { get; } = new(capacity: 32);

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

    public virtual FuncUnit RunAndReturn(IAstNode node)
    {
        NodeIdFuncMap.Clear();
        Run(node);
        return new FuncUnit(NodeIdFuncMap[node.Id]);
    }

    #region General

    /// <inheritdoc />
    public override void Visit(BetweenExpressionNode node)
    {
        var valueAction = NodeIdFuncMap[node.Expression.Id];
        var leftAction = NodeIdFuncMap[node.Left.Id];
        var rightAction = NodeIdFuncMap[node.Right.Id];

        VariantValue Func(VariantValueFuncData data)
        {
            var value = valueAction.Invoke(data);
            var leftValue = leftAction.Invoke(data);
            var rightValue = rightAction.Invoke(data);
            var result = VariantValue.Between(ref value, ref leftValue, ref rightValue, out ErrorCode code);
            ApplyStatistic(code);
            var boolResult = result.AsBoolean;
            return node.IsNot ? new VariantValue(!boolResult) : new VariantValue(boolResult);
        }
        NodeIdFuncMap[node.Id] = Func;
    }

    /// <inheritdoc />
    public override void Visit(BinaryOperationExpressionNode node)
    {
        var leftAction = NodeIdFuncMap[node.Left.Id];
        var rightAction = NodeIdFuncMap[node.Right.Id];
        var operationDelegate = VariantValue.GetOperationDelegate(node.Operation);

        VariantValue Func(VariantValueFuncData data)
        {
            var leftValue = leftAction.Invoke(data);
            var rightValue = rightAction.Invoke(data);
            var result = operationDelegate(ref leftValue, ref rightValue, out ErrorCode code);
            ApplyStatistic(code);
            return result;
        }
        NodeIdFuncMap[node.Id] = Func;
    }

    /// <inheritdoc />
    public override void Visit(CastNode node)
    {
        var expressionAction = NodeIdFuncMap[node.ExpressionNode.Id];

        VariantValue Func(VariantValueFuncData data)
        {
            var expressionValue = expressionAction.Invoke(data);
            if (expressionValue.Cast(node.TargetTypeNode.Type, out VariantValue result))
            {
                return result;
            }
            ApplyStatistic(ErrorCode.CannotCast);
            return VariantValue.Null;
        }
        NodeIdFuncMap[node.Id] = Func;
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

        VariantValue Func(VariantValueFuncData data)
        {
            var leftValue = valueAction.Invoke(data);
            for (int i = 0; i < actions.Length; i++)
            {
                var rightValue = actions[i].Invoke(data);
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
        NodeIdFuncMap[node.Id] = Func;
    }

    /// <inheritdoc />
    public override void Visit(LiteralNode node)
    {
        NodeIdFuncMap[node.Id] = _ => node.Value;
    }

    /// <inheritdoc />
    public override void Visit(ProgramNode node)
    {
        VariantValueFunc programAction = _ => VariantValue.Null;
        foreach (var nodeStatement in node.Statements)
        {
            var result = NodeIdFuncMap[nodeStatement.Id];
            programAction += result;
        }
        NodeIdFuncMap[node.Id] = programAction;
    }

    /// <inheritdoc />
    public override void Visit(UnaryOperationExpressionNode node)
    {
        var action = NodeIdFuncMap[node.Right.Id];

        switch (node.Operation)
        {
            case VariantValue.Operation.Subtract:
                NodeIdFuncMap[node.Id] = context =>
                {
                    var value = action.Invoke(context);
                    var result = VariantValue.Negation(ref value, out ErrorCode code);
                    ApplyStatistic(code);
                    return result;
                };
                break;
            case VariantValue.Operation.Not:
                NodeIdFuncMap[node.Id] = context =>
                {
                    var value = action.Invoke(context);
                    var result = VariantValue.Not(ref value, out ErrorCode code);
                    ApplyStatistic(code);
                    return result;
                };
                break;
            case VariantValue.Operation.IsNull:
                NodeIdFuncMap[node.Id] = context =>
                {
                    var value = action.Invoke(context);
                    return new VariantValue(value.IsNull);
                };
                break;
            case VariantValue.Operation.IsNotNull:
                NodeIdFuncMap[node.Id] = context =>
                {
                    var value = action.Invoke(context);
                    return new VariantValue(!value.IsNull);
                };
                break;
            default:
                throw new QueryCatException(Resources.Errors.InvalidOperation);
        }
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

        var argsDelegatesList = new List<VariantValueFunc>(function.Arguments.Length + 1);
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
                argsDelegatesList.Add(_ => function.Arguments[argPosition].DefaultValue);
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
        NodeIdFuncMap[node.Id] = context =>
        {
            callInfo.Reset();
            callInfo.InvokePushArgs(context);
            return function.Delegate(callInfo);
        };
    }

    #endregion

    private void ApplyStatistic(ErrorCode code)
    {
        if (code != ErrorCode.OK)
        {
            _thread.Statistic.IncrementErrorsCount(code);
        }
    }
}
