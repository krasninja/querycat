using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// The visitor creates <see cref="IRowsIterator" /> as result for <see cref="SelectQuerySpecificationNode" /> node.
/// </summary>
internal sealed partial class SelectSpecificationNodeVisitor : AstVisitor
{
    private const string SourceInputColumn = "source_input_column";

    private readonly ExecutionThread _executionThread;
    private readonly AstTraversal _astTraversal;

    public SelectSpecificationNodeVisitor(ExecutionThread executionThread)
    {
        this._executionThread = executionThread;
        this._astTraversal = new AstTraversal(this);
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        _astTraversal.PostOrder(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        // FROM.
        var context = node.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ResultKey);

        // Misc.
        ApplyStatistic(context);
        SubscribeOnErrorsFromInputSources(context);
        ResolveSelectAllStatement(context.CurrentIterator, node.ColumnsList);
        ResolveSelectSourceColumns(context, node);
        FillQueryContextConditions(node, context);

        // WHERE.
        ApplyFilter(context, node.TableExpression);

        // Fetch remain data.
        CreatePrefetchProjection(context,
            node.GetChildren()
                .Except(new[] { node.TableExpression?.SearchConditionNode })
                .ToArray());

        // GROUP BY/HAVING.
        ApplyAggregate(context, node);
        ApplyHaving(context, node.TableExpression?.HavingNode);

        // ORDER BY.
        ApplyOrderBy(context, node.OrderBy);

        // SELECT.
        CreateSelectRowsSet(context, node.ColumnsList);
        CreateDistinctRowsSet(context, node);

        // OFFSET, FETCH.
        ApplyOffsetFetch(context, node);

        // INTO.
        CreateOutput(context, node);
    }

    #region FROM

    /// <summary>
    /// Update statistic if there is a error in rows input.
    /// </summary>
    private void SubscribeOnErrorsFromInputSources(SelectCommandContext context)
    {
        if (context.RowsInputIterator == null)
        {
            return;
        }
        context.RowsInputIterator.OnError += (sender, args) =>
        {
            _executionThread.Statistic.IncrementErrorsCount(args.ErrorCode, args.RowIndex, args.ColumnIndex);
        };
    }

    private void ApplyStatistic(SelectCommandContext context)
    {
        context.AppendIterator(new StatisticRowsIterator(context.CurrentIterator, _executionThread.Statistic)
        {
            MaxErrorsCount = _executionThread.Options.MaxErrors,
        });
    }

    private void FillQueryContextConditions(
        SelectQuerySpecificationNode querySpecificationNode,
        SelectCommandContext commandContext)
    {
        // Fill conditions.
        foreach (var context in commandContext.InputQueryContextList)
        {
            FillQueryContextConditions(querySpecificationNode, context, commandContext);
        }

        // Fill "limit". For now we limit only of order is not defined.
        if (querySpecificationNode.OrderBy == null && querySpecificationNode.Fetch != null)
        {
            var fetchCount = new SelectCreateDelegateVisitor(_executionThread, commandContext)
                .RunAndReturn(querySpecificationNode.Fetch.CountNode).Invoke().AsInteger;
            foreach (var queryContext in commandContext.InputQueryContextList)
            {
                queryContext.Limit = fetchCount;
            }
        }

        // Fill columns orders.
        if (querySpecificationNode.OrderBy != null)
        {
            foreach (var queryContext in commandContext.InputQueryContextList)
            {
                foreach (var orderNode in querySpecificationNode.OrderBy.OrderBySpecificationNodes)
                {
                    if (orderNode.Expression is not IdentifierExpressionNode identifierExpressionNode)
                    {
                        continue;
                    }
                    var column = queryContext.RowsInput.GetColumnByName(identifierExpressionNode.Name,
                        identifierExpressionNode.SourceName);
                    if (column == null)
                    {
                        continue;
                    }
                    queryContext.Orders.Add(new QueryContextOrder(column, ConvertDirection(orderNode.Order)));
                }
            }
        }
    }

    private void FillQueryContextConditions(
        SelectQuerySpecificationNode querySpecificationNode,
        SelectInputQueryContext rowsInputContext,
        SelectCommandContext commandContext)
    {
        var searchNode = querySpecificationNode.TableExpression?.SearchConditionNode;
        if (searchNode == null)
        {
            return;
        }

        // Process expression <id> <op> <expr> or <expr> <op> <id>.
        bool HandleBinaryOperation(IAstNode node, AstTraversal traversal)
        {
            // Get the binary comparision node.
            if (node is not BinaryOperationExpressionNode binaryOperationExpressionNode
                || !VariantValue.ComparisionOperations.Contains(binaryOperationExpressionNode.Operation))
            {
                return false;
            }
            // Make sure parent does not contain OR condition - it breaks strict AND logic.
            if (traversal.GetParents().OfType<BinaryOperationExpressionNode>().Any(n => n.Operation == VariantValue.Operation.Or))
            {
                return false;
            }
            // Left and Right must be id and expression.
            if (!binaryOperationExpressionNode.MatchType(out IdentifierExpressionNode? identifierNode, out ExpressionNode? expressionNode)
                || expressionNode is IdentifierExpressionNode)
            {
                return false;
            }
            // Try to find correspond row input column.
            var column = identifierNode!.GetAttribute<Column>(AstAttributeKeys.InputColumn);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var makeDelegateVisitor = new SelectCreateDelegateVisitor(_executionThread, commandContext);
            var value = makeDelegateVisitor.RunAndReturn(expressionNode!).Invoke();
            rowsInputContext.Conditions.Add(new QueryContextCondition(column, binaryOperationExpressionNode.Operation, value));
            return true;
        }

        // Process expression <id> BETWEEN <expr> AND <expr>.
        bool HandleBetweenOperation(IAstNode node, AstTraversal traversal)
        {
            // Get the between comparision node.
            if (node is not BetweenExpressionNode betweenExpressionNode)
            {
                return false;
            }
            // Make sure we have id node.
            if (betweenExpressionNode.Expression is not IdentifierExpressionNode identifierNode)
            {
                return false;
            }
            // Try to find correspond row input column.
            var column = identifierNode!.GetAttribute<Column>(AstAttributeKeys.InputColumn);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var makeDelegateVisitor = new SelectCreateDelegateVisitor(_executionThread, commandContext);
            var leftValue = makeDelegateVisitor.RunAndReturn(betweenExpressionNode.Left).Invoke();
            var rightValue = makeDelegateVisitor.RunAndReturn(betweenExpressionNode.Right).Invoke();
            rowsInputContext.Conditions.Add(new QueryContextCondition(column, VariantValue.Operation.GreaterOrEquals, leftValue));
            rowsInputContext.Conditions.Add(new QueryContextCondition(column, VariantValue.Operation.LessOrEquals, rightValue));
            return true;
        }

        var callbackVisitor = new CallbackDelegateVisitor();
        callbackVisitor.Callback = (node, traversal) =>
        {
            if (HandleBinaryOperation(node, traversal))
            {
                return;
            }
            if (HandleBetweenOperation(node, traversal))
            {
                return;
            }
        };
        callbackVisitor.Run(searchNode);
    }

    #endregion

    #region SELECT

    /// <summary>
    /// Assign SourceInputColumn attribute based on rows input iterator.
    /// </summary>
    private void ResolveSelectSourceColumns(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        if (context.RowsInputIterator == null)
        {
            return;
        }

        foreach (var column in querySpecificationNode.ColumnsList.Columns.OfType<SelectColumnsSublistExpressionNode>())
        {
            if (column.ExpressionNode is IdentifierExpressionNode identifierExpressionNode)
            {
                var sourceColumn = context.RowsInputIterator.GetColumnByName(
                    identifierExpressionNode.Name, identifierExpressionNode.SourceName);
                if (sourceColumn != null)
                {
                    column.SetAttribute(SourceInputColumn, sourceColumn);
                }
            }
        }
    }

    private void ResolveSelectAllStatement(IRowsIterator rows, SelectColumnsListNode columnsNode)
    {
        for (int i = 0; i < columnsNode.Columns.Count; i++)
        {
            if (columnsNode.Columns[i] is not SelectColumnsSublistAll)
            {
                continue;
            }

            columnsNode.Columns.Remove(columnsNode.Columns[i]);
            for (int columnIndex = 0; columnIndex < rows.Columns.Length; columnIndex++)
            {
                var column = rows.Columns[columnIndex];

                var astColumn = new SelectColumnsSublistExpressionNode(
                    new IdentifierExpressionNode(column.Name, column.SourceName));
                columnsNode.Columns.Insert(i + columnIndex, astColumn);
            }
        }
    }

    private void CreateSelectRowsSet(
        SelectCommandContext context,
        SelectColumnsListNode columnsNode)
    {
        var selectColumns = CreateSelectColumns(columnsNode).ToList();

        var makeDelegateVisitor = new SelectCreateDelegateVisitor(_executionThread, context);
        var projectedIterator = new ProjectedRowsIterator(context.CurrentIterator);
        foreach (var selectColumn in selectColumns)
        {
            var func = makeDelegateVisitor.RunAndReturn(columnsNode.Columns[selectColumn.ColumnIndex]);
            projectedIterator.AddFuncColumn(selectColumn.Column, func);
        }
        context.AppendIterator(projectedIterator);
    }

    private void CreateDistinctRowsSet(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        if (querySpecificationNode.QuantifierNode.IsDistinct)
        {
            context.CurrentIterator = new DistinctRowsIterator(context.CurrentIterator);
        }
    }

    private record struct ColumnWithIndex(
        Column Column,
        int ColumnIndex);

    private IEnumerable<ColumnWithIndex> CreateSelectColumns(SelectColumnsListNode node)
    {
        string GetColumnName(SelectColumnsSublistNode columnNode)
        {
            if (!string.IsNullOrEmpty(columnNode.Alias))
            {
                return columnNode.Alias;
            }
            if (columnNode is SelectColumnsSublistExpressionNode expressionNode
                && expressionNode.ExpressionNode is IdentifierExpressionNode identifierExpressionNode)
            {
                return identifierExpressionNode.Name;
            }
            if (columnNode is SelectColumnsSublistNameNode selectResultColumnNode)
            {
                return selectResultColumnNode.ColumnName;
            }
            return string.Empty;
        }

        string GetColumnSourceName(SelectColumnsSublistNode columnNode)
        {
            if (columnNode is SelectColumnsSublistExpressionNode expressionNode
                && expressionNode.ExpressionNode is IdentifierExpressionNode identifierExpressionNode)
            {
                return identifierExpressionNode.SourceName;
            }
            if (columnNode is SelectColumnsSublistNameNode selectResultColumnNode)
            {
                return selectResultColumnNode.SourceName;
            }
            return string.Empty;
        }

        for (int i = 0; i < node.Columns.Count; i++)
        {
            var columnNode = node.Columns[i];
            var columnName = GetColumnName(columnNode);
            var columnSourceName = GetColumnSourceName(columnNode);
            if (string.IsNullOrEmpty(columnName) && node.Columns.Count == 1)
            {
                columnName = SingleValueRowsIterator.ColumnTitle;
            }
            var column = !string.IsNullOrEmpty(columnName)
                ? new Column(columnName, columnSourceName, columnNode.GetDataType())
                : new Column(i + 1, columnNode.GetDataType());

            var sourceInputColumn = node.Columns[i].GetAttribute<Column>(SourceInputColumn);
            if (sourceInputColumn != null)
            {
                column.Description = sourceInputColumn.Description;
            }

            yield return new ColumnWithIndex(column, i);
        }
    }

    #endregion

    #region WHERE

    private void ApplyFilter(SelectCommandContext context, SelectTableExpressionNode? selectTableExpressionNode)
    {
        if (selectTableExpressionNode?.SearchConditionNode == null)
        {
            return;
        }

        ResolveNodesTypes(selectTableExpressionNode, context);
        CreatePrefetchProjection(context, selectTableExpressionNode);

        var makeDelegateVisitor = new SelectCreateDelegateVisitor(_executionThread, context);
        var predicate = makeDelegateVisitor.RunAndReturn(selectTableExpressionNode.SearchConditionNode);
        context.AppendIterator(new FilterRowsIterator(context.CurrentIterator, predicate));
    }

    #endregion

    #region ORDER BY

    private void ApplyOrderBy(SelectCommandContext context, SelectOrderByNode? orderByNode)
    {
        if (orderByNode == null)
        {
            return;
        }
        ResolveNodesTypes(orderByNode, context);

        // Create wrapper to initialize rows frame and create index.
        var makeDelegateVisitor = new SelectCreateDelegateVisitor(_executionThread, context);
        var orderFunctions = orderByNode.OrderBySpecificationNodes.Select(n =>
            new OrderRowsIterator.OrderBy(
                makeDelegateVisitor.RunAndReturn(n.Expression),
                ConvertDirection(n.Order),
                n.GetDataType()
            )
        );
        var scope = new VariantValueFuncData(context.CurrentIterator);
        context.AppendIterator(new OrderRowsIterator(scope, orderFunctions.ToArray()));
    }

    private OrderDirection ConvertDirection(SelectOrderSpecification order) => order switch
    {
        SelectOrderSpecification.Ascending => OrderDirection.Ascending,
        SelectOrderSpecification.Descending => OrderDirection.Descending,
        _ => throw new ArgumentOutOfRangeException(nameof(order)),
    };

    #endregion

    #region OFFSET, FETCH

    private void ApplyOffsetFetch(SelectCommandContext context, SelectQuerySpecificationNode querySpecificationNode)
    {
        var makeDelegateVisitor = new SelectCreateDelegateVisitor(_executionThread, context);
        if (querySpecificationNode.Offset != null)
        {
            var count = makeDelegateVisitor.RunAndReturn(querySpecificationNode.Offset.CountNode).Invoke().AsInteger;
            context.CurrentIterator = new OffsetRowsIterator(context.CurrentIterator, count);
        }
        if (querySpecificationNode.Fetch != null)
        {
            var count = makeDelegateVisitor.RunAndReturn(querySpecificationNode.Fetch.CountNode).Invoke().AsInteger;
            context.CurrentIterator = new LimitRowsIterator(context.CurrentIterator, count);
        }
    }

    #endregion

    #region INTO

    private void CreateOutput(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        var queryContext = new RowsOutputQueryContext(context.CurrentIterator.Columns);
        VaryingOutputRowsIterator? outputIterator;
        var hasVaryingTarget = false;
        if (querySpecificationNode.Target != null)
        {
            ResolveNodesTypes(querySpecificationNode.Target, context);
            var makeDelegateVisitor = new SelectCreateDelegateVisitor(_executionThread, context);
            var func = makeDelegateVisitor.RunAndReturn(querySpecificationNode.Target);
            var functionCallInfo = querySpecificationNode.Target
                .GetRequiredAttribute<FunctionCallInfo>(AstAttributeKeys.ArgumentsKey);
            hasVaryingTarget = querySpecificationNode.Target.Arguments.Count > 0;
            outputIterator = new VaryingOutputRowsIterator(
                context.CurrentIterator,
                func,
                functionCallInfo,
                _executionThread.Options.DefaultRowsOutput,
                queryContext);
        }
        else
        {
            outputIterator = new VaryingOutputRowsIterator(
                context.CurrentIterator,
                _executionThread.Options.DefaultRowsOutput,
                queryContext);
        }

        // If we have INTO clause defined we execute iterator and write rows into
        // INTO function rows output. Otherwise we just return IRowsIterator as is and
        // executor writes it into default output.
        if (outputIterator.HasOutputDefined)
        {
            context.HasOutput = true;
            var resultIterator = !hasVaryingTarget
                ? new AdjustColumnsLengthsIterator(outputIterator)
                : (IRowsIterator)outputIterator;
            context.CurrentIterator = new ActionRowsIterator(resultIterator, "write to output")
            {
                BeforeMoveNext = _ =>
                {
                    while (resultIterator.MoveNext())
                    {
                        outputIterator.CurrentOutput.Write(resultIterator.Current);
                    }
                }
            };
        }

        context.HasFinalRowsIterator = true;
    }

    #endregion

    private void CreatePrefetchProjection(SelectCommandContext context, params IAstNode?[] nodes)
    {
        if (context.RowsInputIterator == null)
        {
            return;
        }

        var inputColumnIndexesForSelect = GetColumnsIdsFromNode(context.RowsInputIterator, nodes).ToArray();
        if (inputColumnIndexesForSelect.Length < 1)
        {
            return;
        }

        var fetchIterator = new PrefetchRowsIterator(context.CurrentIterator, context.RowsInputIterator,
            inputColumnIndexesForSelect);
        context.AppendIterator(fetchIterator);
    }

    private void ResolveNodesTypes(IAstNode? node, SelectCommandContext context)
    {
        if (node == null)
        {
            return;
        }
        new SelectResolveTypesVisitor(_executionThread, context).Run(node);
    }

    private void ResolveNodesTypes(IAstNode?[] nodes, SelectCommandContext context)
    {
        var selectResolveTypesVisitor = new SelectResolveTypesVisitor(_executionThread, context);
        foreach (var node in nodes)
        {
            if (node != null)
            {
                selectResolveTypesVisitor.Run(node);
            }
        }
    }

    private ISet<int> GetColumnsIdsFromNode(IRowsIterator rowsIterator, params IAstNode?[] nodes)
    {
        var columns = new HashSet<int>();
        foreach (var node in nodes)
        {
            if (node == null)
            {
                continue;
            }
            foreach (var idNode in node.GetAllChildren<IdentifierExpressionNode>())
            {
                var index = rowsIterator.GetColumnIndexByName(idNode.Name, idNode.SourceName);
                if (index > -1)
                {
                    columns.Add(index);
                }
            }
        }
        return columns;
    }
}
