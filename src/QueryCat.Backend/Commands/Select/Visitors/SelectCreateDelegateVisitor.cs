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
        int columnIndex = _context.GetColumnIndexByName(node.Name, node.SourceName, out var rowsIterator);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            var info = _context.ColumnsInfoContainer.GetByColumn(rowsIterator!.Columns[columnIndex]);
            if (info.Redirect != null)
            {
                columnIndex = rowsIterator.GetColumnIndex(info.Redirect);
            }
            var iterator = rowsIterator;
            NodeIdFuncMap[node.Id] = new FuncUnitFromRowsIterator(iterator, columnIndex);
        }
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistExpressionNode node)
    {
        NodeIdFuncMap[node.Id] = NodeIdFuncMap[node.ExpressionNode.Id];
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistNameNode node)
    {
        int columnIndex = _context.GetColumnIndexByName(node.ColumnName, node.SourceName, out var rowsIterator);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            var iterator = rowsIterator!;
            NodeIdFuncMap[node.Id] = new FuncUnitFromRowsIterator(iterator, columnIndex);
        }
    }

    /// <inheritdoc />
    public override void Visit(SelectExistsExpressionNode node)
    {
        var rowsIterator = node.SubQueryExpressionNode.GetFunc().Invoke().AsObject as IRowsIterator;
        if (rowsIterator == null)
        {
            throw new InvalidOperationException("Incorrect subquery type.");
        }

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
            NodeIdFuncMap[node.Id] = new FuncUnitFromRowsIterator(_context.CurrentIterator, index);
            return;
        }

        base.Visit(node);

        var function = node.GetAttribute<Function>(AstAttributeKeys.FunctionKey);
        if (function == null || !function.IsAggregate)
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

    #region Subqueries

    /// <inheritdoc />
    public override void Visit(SelectQueryExpressionBodyNode node)
    {
        if (NodeIdFuncMap.ContainsKey(node.Id))
        {
            return;
        }

        var rowsIterator = node.GetFunc().Invoke().AsObject as IRowsIterator;
        if (rowsIterator == null)
        {
            throw new InvalidOperationException(Resources.Errors.InvalidRowsInputType);
        }

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

        var rowsIterator = node.SubQueryNode.GetFunc().Invoke().AsObject as IRowsIterator;
        if (rowsIterator == null)
        {
            throw new InvalidOperationException(Resources.Errors.InvalidRowsInputType);
        }
        if (rowsIterator.Columns.Length > 1)
        {
            throw new QueryCatException(Resources.Errors.InvalidSubqueryColumnsCount);
        }
        var operationDelegate = VariantValue.GetOperationDelegate(node.Operation);

        VariantValue AllFunc()
        {
            var leftValue = NodeIdFuncMap[node.Left.Id].Invoke();
            rowsIterator.Reset();
            while (rowsIterator.MoveNext())
            {
                var rightValue = rowsIterator.Current[0];
                var result = operationDelegate(ref leftValue, ref rightValue, out ErrorCode code);
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
                var result = operationDelegate(ref leftValue, ref rightValue, out ErrorCode code);
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
