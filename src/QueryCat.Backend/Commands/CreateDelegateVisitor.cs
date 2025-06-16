using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;
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

        AstTraversal.TypesToIgnore.Add(typeof(Ast.Nodes.Select.SelectQuerySpecificationNode));
        AstTraversal.TypesToIgnore.Add(typeof(Ast.Nodes.Select.SelectQueryCombineNode));
        AstTraversal.TypesToIgnore.Add(typeof(Ast.Nodes.Declare.DeclareNode));
        AstTraversal.TypesToIgnore.Add(typeof(Ast.Nodes.Declare.SetNode));
        AstTraversal.TypesToIgnore.Add(typeof(Ast.Nodes.Insert.InsertNode));
        AstTraversal.TypesToIgnore.Add(typeof(Ast.Nodes.Update.UpdateNode));
        AstTraversal.TypesToIgnore.Add(typeof(Ast.Nodes.Delete.DeleteNode));
        AstTraversal.TypesToIgnore.Add(typeof(Ast.Nodes.For.ForNode));
        AstTraversal.AcceptBeforeIgnore = true;
    }

    /// <inheritdoc />
    public override ValueTask RunAsync(IAstNode node, CancellationToken cancellationToken)
    {
        return AstTraversal.PostOrderAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask<IFuncUnit> RunAndReturnAsync(IAstNode node, CancellationToken cancellationToken)
    {
        if (NodeIdFuncMap.TryGetValue(node.Id, out var funcUnit))
        {
            return funcUnit;
        }
        await RunAsync(node, cancellationToken);
        var handler = NodeIdFuncMap[node.Id];
        NodeIdFuncMap.Clear();
        return handler;
    }

    #region General

    /// <inheritdoc />
    public override async ValueTask VisitAsync(BetweenExpressionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);

        var valueAction = NodeIdFuncMap[node.Expression.Id];
        var leftAction = NodeIdFuncMap[node.Left.Id];
        var rightAction = NodeIdFuncMap[node.Right.Id];

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
        {
            var value = await valueAction.InvokeAsync(thread, ct);
            var leftValue = await leftAction.InvokeAsync(thread, ct);
            var rightValue = await rightAction.InvokeAsync(thread, ct);
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
    public override async ValueTask VisitAsync(BinaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);

        var leftAction = NodeIdFuncMap[node.LeftNode.Id];
        var rightAction = NodeIdFuncMap[node.RightNode.Id];
        NodeIdFuncMap[node.Id] = new BinaryFuncUnit(node.Operation, leftAction, rightAction, node.GetDataType());
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(CaseExpressionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);

        var whenConditions = node.WhenNodes.Select(n => NodeIdFuncMap[n.ConditionNode.Id]).ToArray();
        var whenResults = node.WhenNodes.Select(n => NodeIdFuncMap[n.ResultNode.Id]).ToArray();
        var whenDefault = node.DefaultNode != null
            ? NodeIdFuncMap[node.DefaultNode.Id]
            : new FuncUnitStatic(VariantValue.Null);

        if (node.IsSimpleCase && node.ArgumentNode != null)
        {
            var arg = NodeIdFuncMap[node.ArgumentNode.Id];
            var equalsDelegate = VariantValue.GetEqualsDelegate(node.ArgumentNode.GetDataType());

            async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
            {
                var argValue = await arg.InvokeAsync(thread, ct);
                for (var i = 0; i < whenConditions.Length; i++)
                {
                    var conditionValue = await whenConditions[i].InvokeAsync(thread, ct);
                    if (equalsDelegate.Invoke(in argValue, in conditionValue).AsBoolean)
                    {
                        var resultValue = await whenResults[i].InvokeAsync(thread, ct);
                        return resultValue;
                    }
                }
                return await whenDefault.InvokeAsync(thread, ct);
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
        }
        else if (node.IsSearchCase)
        {
            async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
            {
                for (var i = 0; i < whenConditions.Length; i++)
                {
                    var conditionValue = await whenConditions[i].InvokeAsync(thread, ct);
                    if (conditionValue.AsBoolean)
                    {
                        var resultValue = await whenResults[i].InvokeAsync(thread, ct);
                        return resultValue;
                    }
                }
                return await whenDefault.InvokeAsync(thread, ct);
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
        }
        else
        {
            throw new InvalidOperationException(Resources.Errors.CannotCreateCaseDelegate);
        }
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);

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
    public override async ValueTask VisitAsync(QueryCat.Backend.Ast.Nodes.Select.SelectIdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        await VisitAsync((IdentifierExpressionNode)node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(InOperationExpressionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);

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

        throw new QueryCatException(Resources.Errors.CannotResolveExpressionValueNodes);
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
                    await iterator.ResetAsync(cancellationToken);
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
    public override async ValueTask VisitAsync(LiteralNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        NodeIdFuncMap[node.Id] = new FuncUnitStatic(node.Value);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(ProgramNode node, CancellationToken cancellationToken)
    {
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.Body.Id];
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(ProgramBodyNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        var actions = new IFuncUnit[node.Statements.Count];
        for (var i = 0; i < node.Statements.Count; i++)
        {
            actions[i] = NodeIdFuncMap[node.Statements[i].Id];
        }
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
    public override async ValueTask VisitAsync(UnaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
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
    public override async ValueTask VisitAsync(AtTimeZoneNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        var leftFunc = NodeIdFuncMap[node.LeftNode.Id];
        var tzFunc = NodeIdFuncMap[node.TimeZoneNode.Id];
        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
        {
            var left = (await leftFunc.InvokeAsync(thread, ct)).AsTimestamp;
            var tz = (await tzFunc.InvokeAsync(thread, ct)).AsString;
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

    private sealed class CastFuncUnit(
        IFuncUnit action,
        DataType outputType) : IFuncUnit
    {
        /// <inheritdoc />
        public DataType OutputType => outputType;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            var expressionValue = await action.InvokeAsync(thread, cancellationToken);
            if (!expressionValue.TryCast(outputType, out VariantValue result))
            {
                ApplyStatistic(thread, ErrorCode.CannotCast);
                return VariantValue.Null;
            }
            return result;
        }
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(CastFunctionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        var expressionAction = NodeIdFuncMap[node.ExpressionNode.Id];

        NodeIdFuncMap[node.Id] = new CastFuncUnit(expressionAction, node.GetDataType());
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
    public override async ValueTask VisitAsync(CoalesceFunctionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        var expressionActions = node.Expressions.Select(e => NodeIdFuncMap[e.Id]).ToArray();
        NodeIdFuncMap[node.Id] = new CoalesceFuncUnit(expressionActions, node.GetDataType());
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override async ValueTask VisitAsync(FunctionCallArgumentNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionValueNode.Id];
    }

    private sealed class FunctionCallFuncUnit(
        IFunction function,
        IFuncUnit[] argsUnits,
        DataType outputType) : IFuncUnitArguments
    {
        /// <inheritdoc />
        public DataType OutputType => outputType;

        /// <inheritdoc />
        public IFuncUnit[] ArgumentsUnits => argsUnits;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            using var frame = thread.Stack.CreateFrame();
            foreach (var argsUnit in argsUnits)
            {
                thread.Stack.Push(await argsUnit.InvokeAsync(thread, cancellationToken));
            }
            var result = await FunctionCaller.CallAsync(function.Delegate, thread, cancellationToken);
            return result;
        }
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
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

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(QueryCat.Backend.Ast.Nodes.Select.SelectTableFunctionNode node, CancellationToken cancellationToken)
    {
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.TableFunctionNode.Id];
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Statements

    private async ValueTask VisitSelectQueryNodeAsync(Ast.Nodes.Select.SelectQueryNode node, CancellationToken cancellationToken)
    {
        var handler = await VisitWithStatementVisitor(new Ast.Nodes.Select.SelectStatementNode(node), cancellationToken);
        var iterator = (await handler.InvokeAsync(ExecutionThread, cancellationToken)).AsRequired<IRowsIterator>();
        node.SetDataType(handler.OutputType);
        NodeIdFuncMap[node.Id] = new FuncUnitRowsIteratorScalar(iterator);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(Ast.Nodes.Select.SelectQuerySpecificationNode node, CancellationToken cancellationToken)
    {
        await VisitSelectQueryNodeAsync(node, cancellationToken);
        await base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(Ast.Nodes.Select.SelectQueryCombineNode node, CancellationToken cancellationToken)
    {
        await VisitSelectQueryNodeAsync(node, cancellationToken);
        await base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(Ast.Nodes.Declare.DeclareNode node, CancellationToken cancellationToken)
    {
        await VisitWithStatementVisitor(node, cancellationToken);
        await base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(Ast.Nodes.Declare.SetNode node, CancellationToken cancellationToken)
    {
        await VisitWithStatementVisitor(node, cancellationToken);
        await base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(Ast.Nodes.Insert.InsertNode node, CancellationToken cancellationToken)
    {
        await VisitWithStatementVisitor(node, cancellationToken);
        await base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(Ast.Nodes.Update.UpdateNode node, CancellationToken cancellationToken)
    {
        await VisitWithStatementVisitor(node, cancellationToken);
        await base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(Ast.Nodes.Delete.DeleteNode node, CancellationToken cancellationToken)
    {
        await VisitWithStatementVisitor(node, cancellationToken);
        await base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(Ast.Nodes.For.ForNode node, CancellationToken cancellationToken)
    {
        await VisitWithStatementVisitor(node, cancellationToken);
        await base.VisitAsync(node, cancellationToken);
    }

    private async ValueTask<IFuncUnit> VisitWithStatementVisitor(IAstNode node, CancellationToken cancellationToken)
    {
        var handler = await new StatementsVisitor(ExecutionThread).RunAndReturnAsync(node, cancellationToken);
        NodeIdFuncMap[node.Id] = handler;
        return handler;
    }

    #endregion

    protected static void ApplyStatistic(IExecutionThread thread, ErrorCode code)
    {
        thread.Statistic.AddError(new ExecutionStatistic.RowErrorInfo(code));
    }
}
