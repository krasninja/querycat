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
    private readonly ResolveTypesVisitor _resolveTypesVisitor;

    protected Dictionary<int, IFuncUnit> NodeIdFuncMap { get; } = new(capacity: 32);

    protected ExecutionThread ExecutionThread { get; }

    protected ResolveTypesVisitor ResolveTypesVisitor => _resolveTypesVisitor;

    public CreateDelegateVisitor(ExecutionThread thread) : this(thread, new ResolveTypesVisitor(thread))
    {
    }

    public CreateDelegateVisitor(ExecutionThread thread, ResolveTypesVisitor resolveTypesVisitor)
    {
        ExecutionThread = thread;
        _resolveTypesVisitor = resolveTypesVisitor;
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
        ResolveTypesVisitor.Visit(node);

        var valueAction = NodeIdFuncMap[node.Expression.Id];
        var leftAction = NodeIdFuncMap[node.Left.Id];
        var rightAction = NodeIdFuncMap[node.Right.Id];

        VariantValue Func()
        {
            var value = valueAction.Invoke();
            var leftValue = leftAction.Invoke();
            var rightValue = rightAction.Invoke();
            var result = VariantValue.Between(in value, in leftValue, in rightValue, out ErrorCode code);
            ApplyStatistic(code);
            var boolResult = result.AsBoolean;
            return node.IsNot ? new VariantValue(!boolResult) : new VariantValue(boolResult);
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(BinaryOperationExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);

        var leftAction = NodeIdFuncMap[node.LeftNode.Id];
        var rightAction = NodeIdFuncMap[node.RightNode.Id];
        var action = VariantValue.GetOperationDelegate(node.Operation,
            node.LeftNode.GetDataType(), node.RightNode.GetDataType());

        VariantValue Func()
        {
            var leftValue = leftAction.Invoke();
            var rightValue = rightAction.Invoke();
            var result = action.Invoke(in leftValue, in rightValue);
            return result;
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(CaseExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);

        var whenConditions = node.WhenNodes.Select(n => NodeIdFuncMap[n.ConditionNode.Id]).ToArray();
        var whenResults = node.WhenNodes.Select(n => NodeIdFuncMap[n.ResultNode.Id]).ToArray();
        var whenDefault = node.DefaultNode != null
            ? NodeIdFuncMap[node.DefaultNode.Id]
            : new FuncUnitStatic(VariantValue.Null);

        if (node.IsSimpleCase)
        {
            var arg = NodeIdFuncMap[node.ArgumentNode!.Id];
            var equalsDelegate = VariantValue.GetEqualsDelegate(node.ArgumentNode.GetDataType());

            VariantValue Func()
            {
                var argValue = arg.Invoke();
                for (var i = 0; i < whenConditions.Length; i++)
                {
                    var conditionValue = whenConditions[i].Invoke();
                    if (equalsDelegate.Invoke(in argValue, in conditionValue).AsBoolean)
                    {
                        var resultValue = whenResults[i].Invoke();
                        return resultValue;
                    }
                }
                return whenDefault.Invoke();
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
        }
        else if (node.IsSearchCase)
        {
            VariantValue Func()
            {
                for (var i = 0; i < whenConditions.Length; i++)
                {
                    var conditionValue = whenConditions[i].Invoke();
                    if (conditionValue.AsBoolean)
                    {
                        var resultValue = whenResults[i].Invoke();
                        return resultValue;
                    }
                }
                return whenDefault.Invoke();
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
        }
        else
        {
            throw new InvalidOperationException("Cannot create CASE delegate.");
        }
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);

        if (string.IsNullOrEmpty(node.SourceName))
        {
            var varIndex = ExecutionThread.TopScope.GetVariableIndex(node.Name, out var scope);
            if (varIndex > -1)
            {
                VariantValue Func()
                {
                    return scope!.Variables[varIndex];
                }
                NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
                return;
            }
        }
        throw new CannotFindIdentifierException(node.Name);
    }

    /// <inheritdoc />
    public override void Visit(InOperationExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);

        var actions = node.InExpressionValuesNodes.ValuesNodes.Select(v => NodeIdFuncMap[v.Id]).ToArray();
        var valueAction = NodeIdFuncMap[node.ExpressionNode.Id];
        var equalDelegate = VariantValue.GetEqualsDelegate(node.ExpressionNode.GetDataType());

        VariantValue Func()
        {
            var leftValue = valueAction.Invoke();
            for (int i = 0; i < actions.Length; i++)
            {
                var rightValue = actions[i].Invoke();
                var isEqual = equalDelegate.Invoke(in leftValue, in rightValue);
                if (isEqual.IsNull)
                {
                    continue;
                }
                if (isEqual.AsBoolean)
                {
                    return new VariantValue(!node.IsNot);
                }
            }
            return new VariantValue(node.IsNot);
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(LiteralNode node)
    {
        ResolveTypesVisitor.Visit(node);
        NodeIdFuncMap[node.Id] = new FuncUnitStatic(node.Value);
    }

    /// <inheritdoc />
    public override void Visit(ProgramNode node)
    {
        ResolveTypesVisitor.Visit(node);
        var actions = node.Statements.Select(n => NodeIdFuncMap[n.Id]).ToArray();
        NodeIdFuncMap[node.Id] = new FuncUnitMultiDelegate(DataType.Void, actions);
    }

    /// <inheritdoc />
    public override void Visit(UnaryOperationExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);
        var action = NodeIdFuncMap[node.RightNode.Id];
        var nodeType = node.GetDataType();
        var notDelegate = node.Operation == VariantValue.Operation.Not
            ? VariantValue.GetOperationDelegate(VariantValue.Operation.Not, nodeType)
            : VariantValue.UnaryNullDelegate;

        NodeIdFuncMap[node.Id] = node.Operation switch
        {
            VariantValue.Operation.Subtract => new FuncUnitDelegate(() =>
            {
                var value = action.Invoke();
                var result = VariantValue.Negation(in value, out ErrorCode code);
                ApplyStatistic(code);
                return result;
            }, nodeType),
            VariantValue.Operation.Not => new FuncUnitDelegate(() =>
            {
                var value = action.Invoke();
                var result = notDelegate.Invoke(in value);
                return result;
            }, nodeType),
            VariantValue.Operation.IsNull => new FuncUnitDelegate(() =>
            {
                var value = action.Invoke();
                return new VariantValue(value.IsNull);
            }, nodeType),
            VariantValue.Operation.IsNotNull => new FuncUnitDelegate(() =>
            {
                var value = action.Invoke();
                return new VariantValue(!value.IsNull);
            }, nodeType),
            _ => throw new QueryCatException("Invalid operation.")
        };
    }

    /// <inheritdoc />
    public override void Visit(AtTimeZoneNode node)
    {
        ResolveTypesVisitor.Visit(node);
        VariantValue Func()
        {
            var left = NodeIdFuncMap[node.LeftNode.Id].Invoke();
            var tz = NodeIdFuncMap[node.TimeZoneNode.Id].Invoke();
            try
            {
                var destinationTimestamp = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(left, tz);
                return new VariantValue(destinationTimestamp);
            }
            catch (TimeZoneNotFoundException)
            {
                throw new QueryCatException($"Cannot find time zone '{tz}'.");
            }
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    #endregion

    #region Special functions

    /// <inheritdoc />
    public override void Visit(CastFunctionNode node)
    {
        ResolveTypesVisitor.Visit(node);
        var expressionAction = NodeIdFuncMap[node.ExpressionNode.Id];

        VariantValue Func()
        {
            var expressionValue = expressionAction.Invoke();
            if (expressionValue.TryCast(node.TargetTypeNode.Type, out VariantValue result))
            {
                return result;
            }
            ApplyStatistic(ErrorCode.CannotCast);
            return VariantValue.Null;
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(CoalesceFunctionNode node)
    {
        ResolveTypesVisitor.Visit(node);
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
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override void Visit(FunctionCallArgumentNode node)
    {
        ResolveTypesVisitor.Visit(node);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionValueNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        var function = ResolveTypesVisitor.VisitFunctionCallNode(node);
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
                argsDelegatesList.Add(NodeIdFuncMap[node.Arguments[i].Id]);
                continue;
            }

            // Try optional.
            if (function.Arguments[i].HasDefaultValue)
            {
                argsDelegatesList.Add(new FuncUnitStatic(function.Arguments[i].DefaultValue));
                continue;
            }

            throw new InvalidFunctionArgumentException($"Cannot set argument '{argument.Name}'.");
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
        var callInfo = new FunctionCallInfo(ExecutionThread, argsDelegates);
        node.SetAttribute(AstAttributeKeys.ArgumentsKey, callInfo);
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(() =>
        {
            callInfo.Reset();
            callInfo.InvokePushArgs();
            return function.Delegate(callInfo);
        }, node.GetDataType());
    }

    #endregion

    protected void ApplyStatistic(ErrorCode code)
    {
        if (code != ErrorCode.OK)
        {
            ExecutionThread.Statistic.IncrementErrorsCount(code);
        }
    }
}
