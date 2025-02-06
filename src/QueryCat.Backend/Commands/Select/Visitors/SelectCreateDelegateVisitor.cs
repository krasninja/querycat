using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// Generate delegate for a node using SELECT statement specific processing.
/// </summary>
internal sealed class SelectCreateDelegateVisitor : CreateDelegateVisitor
{
    private readonly SelectCommandContext _context;

    private readonly List<IRowsIterator> _subQueryIterators = new();

    public SelectCreateDelegateVisitor(
        IExecutionThread<ExecutionOptions> thread,
        SelectCommandContext context) : this(thread, context, new SelectResolveTypesVisitor(thread, context))
    {
    }

    /// <inheritdoc />
    public SelectCreateDelegateVisitor(
        IExecutionThread<ExecutionOptions> thread,
        SelectCommandContext context,
        ResolveTypesVisitor resolveTypesVisitor) : base(thread, resolveTypesVisitor)
    {
        _context = context;
        AstTraversal.TypesToIgnore.Add(typeof(SelectQueryNode));
        AstTraversal.AcceptBeforeIgnore = true;
    }

    /// <inheritdoc />
    public override async ValueTask<IFuncUnit> RunAndReturnAsync(IAstNode node, CancellationToken cancellationToken)
    {
        _subQueryIterators.Clear();
        var funcUnit = await base.RunAndReturnAsync(node, cancellationToken);
        if (funcUnit is FuncUnitDelegate funcUnitDelegate)
        {
            funcUnitDelegate.SubQueryIterators = _subQueryIterators;
        }
        return funcUnit;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectIdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        if (VisitIdentifierNode(node, node.TableFieldName, node.TableSourceName))
        {
            return ValueTask.CompletedTask;
        }

        return base.VisitAsync((IdentifierExpressionNode)node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        if (VisitIdentifierNode(node, node.TableFieldName, node.TableSourceName))
        {
            return ValueTask.CompletedTask;
        }

        return base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectColumnsSublistExpressionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectColumnsSublistWindowNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.AggregateFunctionNode.Id];
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectExistsExpressionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        var commandContext = node.SubQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var rowsIterator = commandContext.CurrentIterator;

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
        {
            await rowsIterator.ResetAsync(ct);
            if (await rowsIterator.MoveNextAsync(ct))
            {
                return VariantValue.TrueValue;
            }
            return VariantValue.FalseValue;
        }
        _subQueryIterators.Add(rowsIterator);
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectHavingNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectSearchConditionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectTableFunctionNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.TableFunctionNode.Id];
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);
        if (node.HasAttribute(AstAttributeKeys.InputAggregateIndexKey))
        {
            var index = node.GetAttribute<int>(AstAttributeKeys.InputAggregateIndexKey);
            NodeIdFuncMap[node.Id] = new FuncUnitRowsIteratorColumn(_context.CurrentIterator, index);
            return;
        }

        await base.VisitAsync(node, cancellationToken);

        var function = node.GetAttribute<IFunction>(AstAttributeKeys.FunctionKey);
        if (function is not { IsAggregate: true })
        {
            return;
        }

        var target = CreateAggregateTarget(node, function);
        node.SetAttribute(AstAttributeKeys.AggregateFunctionKey, target);
    }

    private AggregateTarget CreateAggregateTarget(FunctionCallNode node, IFunction function)
    {
        // Try to use alias for column name.
        var columnsSublistNode = AstTraversal.GetFirstParent<SelectColumnsSublistNode>();
        var name = columnsSublistNode != null ? columnsSublistNode.Alias : string.Empty;

        var func = NodeIdFuncMap[node.Id];
        return new AggregateTarget(
            ReturnType: function.ReturnType,
            AggregateFunction: ExecutionThread.FunctionsManager.FindAggregateByName(function.Name),
            ValueGenerator: func,
            Node: node,
            Name: name
        );
    }

    private bool VisitIdentifierNode(IAstNode node, string name, string source)
    {
        if (!_context.TryGetInputSourceByName(name, source, out var result)
            || result == null)
        {
            return false;
        }

        node.SetAttribute(AstAttributeKeys.InputColumnKey, result.Input.Columns[result.ColumnIndex]);
        node.SetDataType(result.Input.Columns[result.ColumnIndex].DataType);

        if (result.Input is IRowsIterator rowsIterator)
        {
            NodeIdFuncMap[node.Id] = new FuncUnitRowsIteratorColumn(rowsIterator, result.ColumnIndex);
            return true;
        }
        if (result.Input is IRowsInput rowsInput)
        {
            NodeIdFuncMap[node.Id] = new FuncUnitRowsInputColumn(rowsInput, result.ColumnIndex);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableValuesNode node, CancellationToken cancellationToken)
    {
        var firstRowTypes = node.RowsNodes.First().ExpressionNodes.Select(n => n.GetDataType());
        var rowsFrame = new RowsFrame(
            firstRowTypes
                .Select((rt, i) => new Column($"column{i + 1}", node.Alias, rt))
                .ToArray()
        );

        node.SetDataType(DataType.Object);

        // Persist rows nodes handlers.
        var handlers = new Dictionary<ExpressionNode, IFuncUnit>(capacity: node.RowsNodes.Count * 4);
        foreach (var rowNode in node.RowsNodes)
        {
            foreach (var expressionNode in rowNode.ExpressionNodes)
            {
                handlers[expressionNode] = NodeIdFuncMap[expressionNode.Id];
            }
        }

        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(async (thread, ct) =>
        {
            if (rowsFrame.IsEmpty)
            {
                // Initialize rows frame.
                var row = new Row(rowsFrame);
                foreach (var rowNode in node.RowsNodes)
                {
                    for (var i = 0; i < rowsFrame.Columns.Length && i < rowNode.ExpressionNodes.Length; i++)
                    {
                        row[i] = await handlers[rowNode.ExpressionNodes[i]].InvokeAsync(thread, ct);
                    }
                    rowsFrame.AddRow(row);
                }
            }
            return VariantValue.CreateFromObject(rowsFrame);
        }, DataType.Object);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(InOperationExpressionNode node, CancellationToken cancellationToken)
    {
        if (node.InExpressionValuesNodes is SelectQueryNode queryNode)
        {
            var valueAction = NodeIdFuncMap[node.ExpressionNode.Id];
            var rowsIterator = await new SelectPlanner(ExecutionThread).CreateIteratorAsync(queryNode, _context, cancellationToken);
            var equalDelegate = VariantValue.GetEqualsDelegate(node.ExpressionNode.GetDataType());

            async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
            {
                var leftValue = await valueAction.InvokeAsync(thread, ct);
                await rowsIterator.ResetAsync(ct);
                while (await rowsIterator.MoveNextAsync(ct))
                {
                    var rightValue = rowsIterator.Current[0];
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

            return;
        }

        await base.VisitAsync(node, cancellationToken);
    }

    #region Subqueries

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectQuerySpecificationNode node, CancellationToken cancellationToken)
    {
        return VisitSelectQueryNodeAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectQueryCombineNode node, CancellationToken cancellationToken)
    {
        return VisitSelectQueryNodeAsync(node, cancellationToken);
    }

    private async ValueTask VisitSelectQueryNodeAsync(SelectQueryNode node, CancellationToken cancellationToken)
    {
        if (NodeIdFuncMap.ContainsKey(node.Id))
        {
            return;
        }

        var rowsIterator = await new SelectPlanner(ExecutionThread).CreateIteratorAsync(node, _context, cancellationToken);
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
        {
            await rowsIterator.ResetAsync(ct);
            if (await rowsIterator.MoveNextAsync(ct))
            {
                return rowsIterator.Current[0];
            }
            return VariantValue.Null;
        }

        _subQueryIterators.Add(rowsIterator);
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectSubqueryConditionExpressionNode node, CancellationToken cancellationToken)
    {
        if (NodeIdFuncMap.ContainsKey(node.Id))
        {
            return;
        }

        var rowsIterator = await new SelectPlanner(ExecutionThread)
            .CreateIteratorAsync(node.SubQueryNode, _context, cancellationToken);
        await ResolveTypesVisitor.VisitAsync(node, cancellationToken);

        if (rowsIterator.Columns.Length > 1)
        {
            throw new QueryCatException(string.Format(Resources.Errors.InvalidSubqueryColumnsCount, rowsIterator.Columns.Length));
        }
        var operationDelegate = VariantValue.GetOperationDelegate(node.Operation);
        var leftValueFunc = NodeIdFuncMap[node.LeftNode.Id];

        async ValueTask<VariantValue> AllFunc(IExecutionThread thread, CancellationToken ct)
        {
            await rowsIterator.ResetAsync(ct);
            while (await rowsIterator.MoveNextAsync(ct))
            {
                var leftValue = await leftValueFunc.InvokeAsync(thread, ct);
                var rightValue = rowsIterator.Current[0];
                var result = operationDelegate(in leftValue, in rightValue, out ErrorCode code);
                ApplyStatistic(thread, code);
                if (!result.AsBoolean)
                {
                    return VariantValue.FalseValue;
                }
            }
            return VariantValue.TrueValue;
        }

        async ValueTask<VariantValue> AnyFunc(IExecutionThread thread, CancellationToken ct)
        {
            await rowsIterator.ResetAsync(ct);
            while (await rowsIterator.MoveNextAsync(ct))
            {
                var leftValue = await leftValueFunc.InvokeAsync(thread, ct);
                var rightValue = rowsIterator.Current[0];
                var result = operationDelegate(in leftValue, in rightValue, out ErrorCode code);
                ApplyStatistic(thread, code);
                if (result.AsBoolean)
                {
                    return VariantValue.TrueValue;
                }
            }
            return VariantValue.FalseValue;
        }

        _subQueryIterators.Add(rowsIterator);
        if (node.Operator == SelectSubqueryConditionExpressionNode.QuantifierOperator.Any)
        {
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(AnyFunc, DataType.Boolean);
        }
        else if (node.Operator == SelectSubqueryConditionExpressionNode.QuantifierOperator.All)
        {
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(AllFunc, DataType.Boolean);
        }
        else
        {
            throw new InvalidOperationException(string.Format(Resources.Errors.InvalidQuantifier, node.Operation));
        }
    }

    #endregion
}
