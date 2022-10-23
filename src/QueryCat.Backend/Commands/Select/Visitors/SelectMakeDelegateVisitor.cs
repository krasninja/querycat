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
internal class SelectMakeDelegateVisitor : MakeDelegateVisitor
{
    private readonly IRowsIterator _rowsIterator;
    private readonly SelectCommandContext _context;

    private readonly List<IRowsIterator> _subQueryIterators = new();

    /// <inheritdoc />
    public SelectMakeDelegateVisitor(ExecutionThread thread, SelectCommandContext context) : base(thread)
    {
        _rowsIterator = context.CurrentIterator;
        _context = context;
    }

    /// <inheritdoc />
    public override FuncUnit RunAndReturn(IAstNode node)
    {
        _subQueryIterators.Clear();
        base.RunAndReturn(node);
        return new FuncUnit(NodeIdFuncMap[node.Id])
        {
            SubQueryIterators = _subQueryIterators.ToArray()
        };
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        int columnIndex = _rowsIterator.GetColumnIndexByName(node.Name, node.SourceName);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            var info = _context.ColumnsInfoContainer.GetByColumn(_rowsIterator.Columns[columnIndex]);
            if (info.Redirect != null)
            {
                columnIndex = _rowsIterator.GetColumnIndex(info.Redirect);
            }
            NodeIdFuncMap[node.Id] = data => data.RowsIterator.Current[columnIndex];
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
        int columnIndex = _rowsIterator.GetColumnIndexByName(node.ColumnName, node.SourceName);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            NodeIdFuncMap[node.Id] = data => data.RowsIterator.Current[columnIndex];
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

        VariantValue Func(VariantValueFuncData data)
        {
            rowsIterator.Reset();
            if (rowsIterator.MoveNext())
            {
                return VariantValue.TrueValue;
            }
            return VariantValue.FalseValue;
        }
        _subQueryIterators.Add(rowsIterator);
        NodeIdFuncMap[node.Id] = Func;
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
            NodeIdFuncMap[node.Id] = data => data.RowsIterator.Current[index];
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
        var functionCallInfo = node.GetAttribute<FunctionCallInfo>(AstAttributeKeys.ArgumentsKey);
        if (functionCallInfo == null)
        {
            throw new InvalidOperationException("Function node must have 'FunctionCallInfo' attribute.");
        }

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
            throw new InvalidOperationException("Incorrect subquery type.");
        }
        if (rowsIterator.Columns.Length > 1)
        {
            throw new QueryCatException(
                string.Format(Resources.Errors.InvalidSubqueryColumnsCount, rowsIterator.Columns.Length));
        }
        VariantValue Func(VariantValueFuncData data)
        {
            rowsIterator.Reset();
            if (rowsIterator.MoveNext())
            {
                return rowsIterator.Current[0];
            }
            return VariantValue.Null;
        }
        _subQueryIterators.Add(rowsIterator);
        NodeIdFuncMap[node.Id] = Func;
    }
}
