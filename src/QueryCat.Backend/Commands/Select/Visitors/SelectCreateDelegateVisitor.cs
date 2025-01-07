using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
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
    public override IFuncUnit RunAndReturn(IAstNode node)
    {
        _subQueryIterators.Clear();
        var funcUnit = base.RunAndReturn(node);
        if (funcUnit is FuncUnitDelegate funcUnitDelegate)
        {
            funcUnitDelegate.SubQueryIterators = _subQueryIterators;
        }
        return funcUnit;
    }

    /// <inheritdoc />
    public override void Visit(SelectIdentifierExpressionNode node)
    {
        if (VisitIdentifierNode(node, node.TableFieldName, node.TableSourceName))
        {
            return;
        }

        base.Visit((IdentifierExpressionNode)node);
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        if (VisitIdentifierNode(node, node.TableFieldName, node.TableSourceName))
        {
            return;
        }

        base.Visit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistWindowNode node)
    {
        ResolveTypesVisitor.Visit(node);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.AggregateFunctionNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(SelectExistsExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);
        var commandContext = node.SubQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var rowsIterator = commandContext.CurrentIterator;

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken cancellationToken)
        {
            rowsIterator.Reset();
            if (await rowsIterator.MoveNextAsync(cancellationToken))
            {
                return VariantValue.TrueValue;
            }
            return VariantValue.FalseValue;
        }
        _subQueryIterators.Add(rowsIterator);
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(SelectHavingNode node)
    {
        ResolveTypesVisitor.Visit(node);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(SelectSearchConditionNode node)
    {
        ResolveTypesVisitor.Visit(node);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        ResolveTypesVisitor.Visit(node);
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.TableFunctionNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        ResolveTypesVisitor.Visit(node);
        if (node.HasAttribute(AstAttributeKeys.InputAggregateIndexKey))
        {
            var index = node.GetAttribute<int>(AstAttributeKeys.InputAggregateIndexKey);
            NodeIdFuncMap[node.Id] = new FuncUnitRowsIteratorColumn(_context.CurrentIterator, index);
            return;
        }

        base.Visit(node);

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
    public override void Visit(SelectTableValuesNode node)
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
    }

    /// <inheritdoc />
    public override void Visit(InOperationExpressionNode node)
    {
        if (node.InExpressionValuesNodes is SelectQueryNode queryNode)
        {
            var valueAction = NodeIdFuncMap[node.ExpressionNode.Id];
            var rowsIterator = new SelectPlanner(ExecutionThread).CreateIterator(queryNode, _context);
            var equalDelegate = VariantValue.GetEqualsDelegate(node.ExpressionNode.GetDataType());

            async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken cancellationToken)
            {
                var leftValue = await valueAction.InvokeAsync(thread, cancellationToken);
                rowsIterator.Reset();
                while (await rowsIterator.MoveNextAsync(cancellationToken))
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

        base.Visit(node);
    }

    #region Subqueries

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node) => VisitSelectQueryNode(node);

    /// <inheritdoc />
    public override void Visit(SelectQueryCombineNode node) => VisitSelectQueryNode(node);

    private void VisitSelectQueryNode(SelectQueryNode node)
    {
        if (NodeIdFuncMap.ContainsKey(node.Id))
        {
            return;
        }

        var rowsIterator = new SelectPlanner(ExecutionThread).CreateIterator(node, _context);
        ResolveTypesVisitor.Visit(node);

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken cancellationToken)
        {
            rowsIterator.Reset();
            if (await rowsIterator.MoveNextAsync(cancellationToken))
            {
                return rowsIterator.Current[0];
            }
            return VariantValue.Null;
        }

        _subQueryIterators.Add(rowsIterator);
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(SelectSubqueryConditionExpressionNode node)
    {
        if (NodeIdFuncMap.ContainsKey(node.Id))
        {
            return;
        }

        var rowsIterator = new SelectPlanner(ExecutionThread).CreateIterator(node.SubQueryNode, _context);
        ResolveTypesVisitor.Visit(node);

        if (rowsIterator.Columns.Length > 1)
        {
            throw new QueryCatException(string.Format(Resources.Errors.InvalidSubqueryColumnsCount, rowsIterator.Columns.Length));
        }
        var operationDelegate = VariantValue.GetOperationDelegate(node.Operation);
        var leftValueFunc = NodeIdFuncMap[node.LeftNode.Id];

        async ValueTask<VariantValue> AllFunc(IExecutionThread thread, CancellationToken cancellationToken)
        {
            rowsIterator.Reset();
            while (await rowsIterator.MoveNextAsync(cancellationToken))
            {
                var leftValue = await leftValueFunc.InvokeAsync(thread, cancellationToken);
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

        async ValueTask<VariantValue> AnyFunc(IExecutionThread thread, CancellationToken cancellationToken)
        {
            rowsIterator.Reset();
            while (await rowsIterator.MoveNextAsync(cancellationToken))
            {
                var leftValue = await leftValueFunc.InvokeAsync(thread, cancellationToken);
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
