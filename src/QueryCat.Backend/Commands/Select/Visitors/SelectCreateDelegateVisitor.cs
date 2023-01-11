using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// Generate delegate for a node using SELECT statement specific processing.
/// </summary>
internal class SelectCreateDelegateVisitor : CreateDelegateVisitor
{
    private readonly SelectCommandContext _context;

    private readonly List<IRowsIterator> _subQueryIterators = new();

    /// <inheritdoc />
    public SelectCreateDelegateVisitor(ExecutionThread thread, SelectCommandContext context) : base(thread)
    {
        _context = context;
        AstTraversal.TypesToIgnore.Add(typeof(SelectQuerySpecificationNode));
        AstTraversal.AcceptBeforeIgnore = true;
    }

    /// <inheritdoc />
    public override IFuncUnit RunAndReturn(IAstNode node)
    {
        _subQueryIterators.Clear();
        base.RunAndReturn(node);
        var funcUnit = NodeIdFuncMap[node.Id];
        funcUnit.SetData(FuncUnit.SubqueriesRowsIterators, _subQueryIterators);
        return funcUnit;
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        if (VisitIdentifierNode(node, node.Name, node.SourceName))
        {
            return;
        }

        base.Visit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistExpressionNode node)
    {
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(SelectExistsExpressionNode node)
    {
        var commandContext = node.SubQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var rowsIterator = commandContext.CurrentIterator;

        VariantValue Func()
        {
            rowsIterator.Reset();
            if (rowsIterator.MoveNext())
            {
                return VariantValue.TrueValue;
            }
            return VariantValue.FalseValue;
        }
        _subQueryIterators.Add(rowsIterator);
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func);
    }

    /// <inheritdoc />
    public override void Visit(SelectHavingNode node)
    {
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(SelectSearchConditionNode node)
    {
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.TableFunction.Id];
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        if (node.HasAttribute(AstAttributeKeys.InputAggregateIndexKey))
        {
            var index = node.GetAttribute<int>(AstAttributeKeys.InputAggregateIndexKey);
            NodeIdFuncMap[node.Id] = new FuncUnitRowsIteratorColumn(_context.CurrentIterator, index);
            return;
        }

        base.Visit(node);

        var function = node.GetAttribute<Function>(AstAttributeKeys.FunctionKey);
        if (function is not { IsAggregate: true })
        {
            return;
        }

        var target = CreateAggregateTarget(node, function);
        node.SetAttribute(AstAttributeKeys.AggregateFunctionKey, target);
    }

    private AggregateTarget CreateAggregateTarget(FunctionCallNode node, Function function)
    {
        var functionCallInfo = node.GetRequiredAttribute<FunctionCallInfo>(AstAttributeKeys.ArgumentsKey);

        // Try to use alias for column name.
        var columnsSublistNode = AstTraversal.GetFirstParent<SelectColumnsSublistNode>();
        var name = columnsSublistNode != null ? columnsSublistNode.Alias : string.Empty;

        var func = NodeIdFuncMap[node.Id];
        return new AggregateTarget(
            ReturnType: function.ReturnType,
            AggregateFunction: ExecutionThread.FunctionsManager.FindAggregateByName(function.Name),
            FunctionCallInfo: functionCallInfo,
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

        if (result.Input is IRowsIterator rowsIterator)
        {
            var info = _context.ColumnsInfoContainer.GetByColumn(rowsIterator.Columns[result.ColumnIndex]);
            var index = result.ColumnIndex;
            if (info.Redirect != null)
            {
                index = rowsIterator.GetColumnIndex(info.Redirect);
            }
            NodeIdFuncMap[node.Id] = new FuncUnitRowsIteratorColumn(rowsIterator, index);
            return true;
        }
        if (result.Input is IRowsInput rowsInput)
        {
            NodeIdFuncMap[node.Id] = new FuncUnitRowsInputColumn(rowsInput, result.ColumnIndex);
            return true;
        }

        return false;
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

        var commandContext = node.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var rowsIterator = commandContext.CurrentIterator;

        VariantValue Func()
        {
            rowsIterator.Reset();
            if (rowsIterator.MoveNext())
            {
                return rowsIterator.Current[0];
            }
            return VariantValue.Null;
        }

        _subQueryIterators.Add(rowsIterator);
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func);
    }

    /// <inheritdoc />
    public override void Visit(SelectSubqueryConditionExpressionNode node)
    {
        if (NodeIdFuncMap.ContainsKey(node.Id))
        {
            return;
        }

        var commandContext = node.SubQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var rowsIterator = commandContext.CurrentIterator;

        if (rowsIterator.Columns.Length > 1)
        {
            throw new QueryCatException($"Subquery returns {rowsIterator.Columns.Length} columns, expected 1.");
        }
        var operationDelegate = VariantValue.GetOperationDelegate(node.Operation);

        VariantValue AllFunc()
        {
            var leftValue = NodeIdFuncMap[node.Left.Id].Invoke();
            rowsIterator.Reset();
            while (rowsIterator.MoveNext())
            {
                var rightValue = rowsIterator.Current[0];
                var result = operationDelegate(in leftValue, in rightValue, out ErrorCode code);
                ApplyStatistic(code);
                if (!result.AsBoolean)
                {
                    return VariantValue.FalseValue;
                }
            }
            return VariantValue.TrueValue;
        }

        VariantValue AnyFunc()
        {
            var leftValue = NodeIdFuncMap[node.Left.Id].Invoke();
            rowsIterator.Reset();
            while (rowsIterator.MoveNext())
            {
                var rightValue = rowsIterator.Current[0];
                var result = operationDelegate(in leftValue, in rightValue, out ErrorCode code);
                ApplyStatistic(code);
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
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(AnyFunc);
        }
        else if (node.Operator == SelectSubqueryConditionExpressionNode.QuantifierOperator.All)
        {
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(AllFunc);
        }
        else
        {
            throw new InvalidOperationException($"Quantifier '{node.Operator}' cannot be proceed.");
        }
    }

    #endregion
}
