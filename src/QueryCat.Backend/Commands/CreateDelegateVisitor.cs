using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Generate delegate for the specific node.
/// </summary>
internal partial class CreateDelegateVisitor : AstVisitor
{
    private const string ObjectSelectorKey = "object_selector_key";

    private const string ObjectSelectorContainerKey = "object_selector_container_key";

    private readonly ResolveTypesVisitor _resolveTypesVisitor;

    protected Dictionary<int, IFuncUnit> NodeIdFuncMap { get; } = new(capacity: 32);

    protected IExecutionThread<ExecutionOptions> ExecutionThread { get; }

    protected ResolveTypesVisitor ResolveTypesVisitor => _resolveTypesVisitor;

    public CreateDelegateVisitor(IExecutionThread<ExecutionOptions> thread) : this(thread, new ResolveTypesVisitor(thread))
    {
    }

    public CreateDelegateVisitor(IExecutionThread<ExecutionOptions> thread, ResolveTypesVisitor resolveTypesVisitor)
    {
        ExecutionThread = thread;
        _resolveTypesVisitor = resolveTypesVisitor;
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        AstTraversal.PostOrder(node);
    }

    /// <inheritdoc />
    public override IFuncUnit RunAndReturn(IAstNode node)
    {
        if (NodeIdFuncMap.TryGetValue(node.Id, out var funcUnit))
        {
            return funcUnit;
        }
        Run(node);
        var handler = NodeIdFuncMap[node.Id];
        NodeIdFuncMap.Clear();
        return handler;
    }

    #region General

    /// <inheritdoc />
    public override void Visit(BetweenExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);

        var valueAction = NodeIdFuncMap[node.Expression.Id];
        var leftAction = NodeIdFuncMap[node.Left.Id];
        var rightAction = NodeIdFuncMap[node.Right.Id];

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken cancellationToken)
        {
            var value = await valueAction.InvokeAsync(thread, cancellationToken);
            var leftValue = await leftAction.InvokeAsync(thread, cancellationToken);
            var rightValue = await rightAction.InvokeAsync(thread, cancellationToken);
            var result = VariantValue.Between(in value, in leftValue, in rightValue, out ErrorCode code);
            ApplyStatistic(thread, code);
            var boolResult = result.AsBoolean;
            return node.IsNot ? new VariantValue(!boolResult) : new VariantValue(boolResult);
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    private sealed class BinaryFuncUnit(
        VariantValue.Operation operation,
        IFuncUnit leftAction,
        IFuncUnit rightAction,
        DataType outputType) : IFuncUnit
    {
        /// <inheritdoc />
        public DataType OutputType => outputType;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            var leftValue = await leftAction.InvokeAsync(thread, cancellationToken);
            var rightValue = await rightAction.InvokeAsync(thread, cancellationToken);
            var operationDelegate = VariantValue.GetOperationDelegate(operation, leftValue.Type, rightValue.Type);
            return operationDelegate.Invoke(in leftValue, in rightValue);
        }
    }

    /// <inheritdoc />
    public override void Visit(BinaryOperationExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);

        var leftAction = NodeIdFuncMap[node.LeftNode.Id];
        var rightAction = NodeIdFuncMap[node.RightNode.Id];
        NodeIdFuncMap[node.Id] = new BinaryFuncUnit(node.Operation, leftAction, rightAction, node.GetDataType());
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

            async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken cancellationToken)
            {
                var argValue = await arg.InvokeAsync(thread, cancellationToken);
                for (var i = 0; i < whenConditions.Length; i++)
                {
                    var conditionValue = await whenConditions[i].InvokeAsync(thread, cancellationToken);
                    if (equalsDelegate.Invoke(in argValue, in conditionValue).AsBoolean)
                    {
                        var resultValue = await whenResults[i].InvokeAsync(thread, cancellationToken);
                        return resultValue;
                    }
                }
                return await whenDefault.InvokeAsync(thread, cancellationToken);
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
        }
        else if (node.IsSearchCase)
        {
            async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken cancellationToken)
            {
                for (var i = 0; i < whenConditions.Length; i++)
                {
                    var conditionValue = await whenConditions[i].InvokeAsync(thread, cancellationToken);
                    if (conditionValue.AsBoolean)
                    {
                        var resultValue = await whenResults[i].InvokeAsync(thread, cancellationToken);
                        return resultValue;
                    }
                }
                return await whenDefault.InvokeAsync(thread, cancellationToken);
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

        if (ExecutionThread.ContainsVariable(node.Name))
        {
            var context = new ObjectSelectorContext();
            node.SetAttribute(ObjectSelectorKey, context);
            var strategies = GetObjectSelectStrategies(node, NodeIdFuncMap);
            NodeIdFuncMap[node.Id] = new ObjectSelectFuncUnit(node.Name, node.GetDataType(), strategies, context);
            return;
        }

        if (node.IsCurrentSpecialIdentifier
            && AstTraversal.GetFirstParent<IdentifierExpressionNode>() is { } parentObjectNode)
        {
            var container = new VariantValueContainer();
            parentObjectNode.SetAttribute(ObjectSelectorContainerKey, container);
            var context = new ObjectSelectorContext();
            node.SetAttribute(ObjectSelectorKey, context);
            var strategies = GetObjectSelectStrategies(node, NodeIdFuncMap);
            NodeIdFuncMap[node.Id] = new ObjectSelectSpecialFuncUnit(container, strategies, context);
            return;
        }

        throw new CannotFindIdentifierException(node.Name);
    }

    /// <inheritdoc />
    public override void Visit(InOperationExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);

        var valueAction = NodeIdFuncMap[node.ExpressionNode.Id];
        if (node.InExpressionValuesNodes is InExpressionValuesNode inExpressionValuesNode)
        {
            var actions = inExpressionValuesNode.ValuesNodes.Select(v => NodeIdFuncMap[v.Id]).ToArray();
            NodeIdFuncMap[node.Id] = new InArrayFuncUnit(valueAction, actions, node.IsNot, node.GetDataType());
            return;
        }
        if (node.InExpressionValuesNodes is IdentifierExpressionNode identifierExpressionNode)
        {
            var action = NodeIdFuncMap[identifierExpressionNode.Id];
            NodeIdFuncMap[node.Id] = new InArrayFuncUnit(valueAction, [action], node.IsNot, node.GetDataType());
            return;
        }

        throw new QueryCatException("Cannot resolve expression values node.");
    }

    private sealed class InArrayFuncUnit(
        IFuncUnit leftAction,
        IFuncUnit[] funcUnits,
        bool isNot,
        DataType outputType) : IFuncUnit
    {
        /// <inheritdoc />
        public DataType OutputType => outputType;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            var leftValue = await leftAction.InvokeAsync(thread, cancellationToken);
            foreach (var funcUnit in funcUnits)
            {
                var rightValue = await funcUnit.InvokeAsync(thread, cancellationToken);

                VariantValue isEqual;
                if (rightValue.Type == DataType.Object)
                {
                    var iterator = RowsIteratorConverter.Convert(rightValue);
                    while (await iterator.MoveNextAsync(cancellationToken))
                    {
                        var iteratorValue = iterator.Current[0];
                        isEqual = VariantValue.Equals(in leftValue, in iteratorValue, out _);
                        if (isEqual.IsNull)
                        {
                            continue;
                        }
                        if (isEqual.AsBoolean)
                        {
                            return new VariantValue(!isNot);
                        }
                    }
                }
                else
                {
                    isEqual = VariantValue.Equals(in leftValue, in rightValue, out _);
                    if (isEqual.IsNull)
                    {
                        continue;
                    }
                    if (isEqual.AsBoolean)
                    {
                        return new VariantValue(!isNot);
                    }
                }
            }
            return new VariantValue(isNot);
        }
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

    private sealed class UnarySubtractFuncUnit(
        IFuncUnit action,
        DataType outputType) : IFuncUnit
    {
        /// <inheritdoc />
        public DataType OutputType => outputType;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            var value = await action.InvokeAsync(thread, cancellationToken);
            var result = VariantValue.Negation(in value, out ErrorCode _);
            return result;
        }
    }

    private sealed class UnaryNotFuncUnit(
        IFuncUnit action,
        DataType outputType) : IFuncUnit
    {
        /// <inheritdoc />
        public DataType OutputType => outputType;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            var value = await action.InvokeAsync(thread, cancellationToken);
            var notDelegate = VariantValue.GetOperationDelegate(VariantValue.Operation.Not, value.Type);
            return notDelegate.Invoke(value);
        }
    }

    private sealed class UnaryIsNullFuncUnit(
        IFuncUnit action,
        DataType outputType) : IFuncUnit
    {
        /// <inheritdoc />
        public DataType OutputType => outputType;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            var value = await action.InvokeAsync(thread, cancellationToken);
            return new VariantValue(value.IsNull);
        }
    }

    private sealed class UnaryIsNotNullFuncUnit(
        IFuncUnit action,
        DataType outputType) : IFuncUnit
    {
        /// <inheritdoc />
        public DataType OutputType => outputType;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            var value = await action.InvokeAsync(thread, cancellationToken);
            return new VariantValue(!value.IsNull);
        }
    }

    /// <inheritdoc />
    public override void Visit(UnaryOperationExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);
        var action = NodeIdFuncMap[node.RightNode.Id];
        var nodeType = node.GetDataType();

        NodeIdFuncMap[node.Id] = node.Operation switch
        {
            VariantValue.Operation.Subtract => new UnarySubtractFuncUnit(action, nodeType),
            VariantValue.Operation.Not => new UnaryNotFuncUnit(action, nodeType),
            VariantValue.Operation.IsNull => new UnaryIsNullFuncUnit(action, nodeType),
            VariantValue.Operation.IsNotNull => new UnaryIsNotNullFuncUnit(action, nodeType),
            _ => throw new QueryCatException(Resources.Errors.InvalidOperation),
        };
    }

    /// <inheritdoc />
    public override void Visit(AtTimeZoneNode node)
    {
        ResolveTypesVisitor.Visit(node);
        var leftFunc = NodeIdFuncMap[node.LeftNode.Id];
        var tzFunc = NodeIdFuncMap[node.TimeZoneNode.Id];
        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken cancellationToken)
        {
            var left = (await leftFunc.InvokeAsync(thread, cancellationToken)).AsTimestamp;
            var tz = (await tzFunc.InvokeAsync(thread, cancellationToken)).AsString;
            if (!left.HasValue)
            {
                return VariantValue.Null;
            }
            try
            {
                var destinationTimestamp = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(left.Value, tz);
                return new VariantValue(destinationTimestamp);
            }
            catch (TimeZoneNotFoundException)
            {
                throw new QueryCatException(string.Format(Resources.Errors.CannotFindTimeZone, tz));
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

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken cancellationToken)
        {
            var expressionValue = await expressionAction.InvokeAsync(thread, cancellationToken);
            if (expressionValue.TryCast(node.TargetTypeNode.Type, out VariantValue result))
            {
                return result;
            }
            ApplyStatistic(thread, ErrorCode.CannotCast);
            return VariantValue.Null;
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    private sealed class CoalesceFuncUnit : IFuncUnit
    {
        private readonly IFuncUnit[] _actions;

        /// <inheritdoc />
        public DataType OutputType { get; }

        public CoalesceFuncUnit(IFuncUnit[] actions, DataType outputType)
        {
            _actions = actions;
            OutputType = outputType;
        }

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            foreach (var expressionAction in _actions)
            {
                var value = await expressionAction.InvokeAsync(thread, cancellationToken);
                if (!value.IsNull)
                {
                    return value;
                }
            }
            return VariantValue.Null;
        }
    }

    /// <inheritdoc />
    public override void Visit(CoalesceFunctionNode node)
    {
        ResolveTypesVisitor.Visit(node);
        var expressionActions = node.Expressions.Select(e => NodeIdFuncMap[e.Id]).ToArray();
        NodeIdFuncMap[node.Id] = new CoalesceFuncUnit(expressionActions, node.GetDataType());
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override void Visit(FunctionCallArgumentNode node)
    {
        ResolveTypesVisitor.Visit(node);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionValueNode.Id];
    }

    private sealed class FunctionCallFuncUnit(
        IFunction function,
        IFuncUnit[] argsUnits,
        DataType outputType) : IFuncUnit
    {
        /// <inheritdoc />
        public DataType OutputType => outputType;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            thread.Stack.CreateFrame();
            foreach (var argsUnit in argsUnits)
            {
                thread.Stack.Push(await argsUnit.InvokeAsync(thread, cancellationToken));
            }
            var result = function.Delegate(thread);
            thread.Stack.CloseFrame();
            return result;
        }
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        var function = ResolveTypesVisitor.VisitFunctionCallNode(node);
        if (ExecutionThread.Options.SafeMode && !function.IsSafe)
        {
            throw new SafeModeException(string.Format(Resources.Errors.CannotUseUnsafeFunction, function.Name));
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
                argsDelegatesList.Add(NodeIdFuncMap[node.Arguments[i].Id]);
                continue;
            }

            // Try optional.
            if (argument.HasDefaultValue)
            {
                argsDelegatesList.Add(new FuncUnitStatic(argument.DefaultValue));
                continue;
            }

            throw new InvalidFunctionArgumentException(string.Format(Resources.Errors.CannotSetArgument, argument.Name));
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
        var callInfo = new FuncUnitCallInfo(argsDelegates);
        node.SetAttribute(AstAttributeKeys.ArgumentsKey, callInfo);
        NodeIdFuncMap[node.Id] = new FunctionCallFuncUnit(function, argsDelegates, node.GetDataType());
    }

    #endregion

    protected static void ApplyStatistic(IExecutionThread thread, ErrorCode code)
    {
        thread.Statistic.AddError(new ExecutionStatistic.RowErrorInfo(code));
    }
}
