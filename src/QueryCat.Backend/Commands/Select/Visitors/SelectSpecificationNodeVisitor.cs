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
internal sealed partial class SelectSpecificationNodeVisitor : SelectAstVisitor
{
    private const string SourceInputColumn = "source_input_column";

    public SelectSpecificationNodeVisitor(ExecutionThread executionThread) : base(executionThread)
    {
        AstTraversal.TypesToIgnore.Add(typeof(SelectQuerySpecificationNode));
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        AstTraversal.PostOrder(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        // FROM.
        var context = node.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        if (context.HasFinalRowsIterator)
        {
            return;
        }

        // Misc.
        ApplyStatistic(context);
        SubscribeOnErrorsFromInputSources(context);
        ResolveSelectAllStatement(context.CurrentIterator, node.ColumnsList);
        ResolveSelectSourceColumns(context, node);

        // WHERE.
        ApplyFilter(context, node.TableExpression);

        // Fetch remain data.
        CreatePrefetchProjection(context, new List<IAstNode?>
        {
            node.ColumnsList, node.Target, node.DistinctNode, node.OrderBy, node.Offset, node.Fetch,
            node.TableExpression
        });

        // GROUP BY/HAVING.
        ApplyAggregate(context, node);
        ApplyHaving(context, node.TableExpression?.HavingNode);

        // SELECT.
        AddSelectRowsSet(context, node.ColumnsList);
        FillQueryContextConditions(node, context);

        // DISTINCT.
        CreateDistinctRowsSet(context, node);

        // ORDER BY.
        ApplyOrderBy(context, node.OrderBy);
        SetSelectRowsSet(context, node.ColumnsList);

        // OFFSET, FETCH.
        ApplyOffsetFetch(context, node.Offset, node.Fetch);

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
            ExecutionThread.Statistic.IncrementErrorsCount(args.ErrorCode, args.RowIndex, args.ColumnIndex);
        };
    }

    private void ApplyStatistic(SelectCommandContext context)
    {
        context.SetIterator(new StatisticRowsIterator(context.CurrentIterator, ExecutionThread.Statistic)
        {
            MaxErrorsCount = ExecutionThread.Options.MaxErrors,
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
            var fetchCount = new SelectCreateDelegateVisitor(ExecutionThread, commandContext)
                .RunAndReturn(querySpecificationNode.Fetch.CountNode).Invoke().AsInteger;
            foreach (var queryContext in commandContext.InputQueryContextList)
            {
                queryContext.QueryInfo.Limit = fetchCount;
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

        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, commandContext);

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
            if (!binaryOperationExpressionNode.MatchType(out IdentifierExpressionNode? identifierNode, out ExpressionNode? expressionNode))
            {
                return false;
            }
            // Try to find correspond row input column.
            var column = identifierNode!.GetAttribute<Column>(AstAttributeKeys.InputColumn);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var valueFunc = makeDelegateVisitor.RunAndReturn(expressionNode!);
            rowsInputContext.QueryInfo.AddCondition(column, binaryOperationExpressionNode.Operation, valueFunc);
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
            var column = identifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumn);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var leftValueFunc = makeDelegateVisitor.RunAndReturn(betweenExpressionNode.Left);
            var rightValueFunc = makeDelegateVisitor.RunAndReturn(betweenExpressionNode.Right);
            rowsInputContext.QueryInfo.AddCondition(column, VariantValue.Operation.GreaterOrEquals, leftValueFunc);
            rowsInputContext.QueryInfo.AddCondition(column, VariantValue.Operation.LessOrEquals, rightValueFunc);
            return true;
        }

        bool HandleInOperation(IAstNode node, AstTraversal traversal)
        {
            // Get the IN comparision node.
            if (node is not InOperationExpressionNode inOperationExpressionNode)
            {
                return false;
            }
            // Make sure we have id node.
            if (inOperationExpressionNode.Expression is not IdentifierExpressionNode identifierNode)
            {
                return false;
            }
            // Try to find correspond row input column.
            var column = identifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumn);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var values = new List<IFuncUnit>();
            foreach (var inExpressionValue in inOperationExpressionNode.InExpressionValues.Values)
            {
                values.Add(makeDelegateVisitor.RunAndReturn(inExpressionValue));
            }
            rowsInputContext.QueryInfo.AddCondition(column, VariantValue.Operation.In, values.ToArray());
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
            if (HandleInOperation(node, traversal))
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

    private void AddSelectRowsSet(
        SelectCommandContext context,
        SelectColumnsListNode columnsNode)
    {
        var selectColumns = CreateSelectColumns(columnsNode).ToList();
        var projectedIterator = new ProjectedRowsIterator(context.CurrentIterator);

        var iterator = context.CurrentIterator;
        for (var i = 0; i < context.CurrentIterator.Columns.Length; i++)
        {
            var index = i;
            projectedIterator.AddFuncColumn(
                context.CurrentIterator.Columns[index], new FuncUnitFromRowsIterator(iterator, index));
        }

        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, context);
        foreach (var selectColumn in selectColumns)
        {
            var func = makeDelegateVisitor.RunAndReturn(columnsNode.Columns[selectColumn.ColumnIndex]);
            var columnIndex = projectedIterator.AddFuncColumn(selectColumn.Column, func);
            var info = context.ColumnsInfoContainer.GetByColumn(projectedIterator.Columns[columnIndex]);
            info.RelatedSelectSublistNode = columnsNode.Columns[selectColumn.ColumnIndex];
        }
        context.SetIterator(projectedIterator);
    }

    private void SetSelectRowsSet(
        SelectCommandContext context,
        SelectColumnsListNode columnsNode)
    {
        var projectedIterator = new ProjectedRowsIterator(context.CurrentIterator);
        var iterator = context.CurrentIterator;
        foreach (var selectColumn in columnsNode.Columns)
        {
            var columnInfo = context.ColumnsInfoContainer.Columns
                .FirstOrDefault(c => c.RelatedSelectSublistNode == selectColumn);
            if (columnInfo == null)
            {
                continue;
            }

            var index = -1;
            for (var i = 0; i < iterator.Columns.Length; i++)
            {
                if (columnInfo.Column == iterator.Columns[i])
                {
                    index = i;
                    break;
                }
            }
            if (index > -1)
            {
                projectedIterator.AddFuncColumn(columnInfo.Column, new FuncUnitFromRowsIterator(iterator, index));
            }
        }
        context.SetIterator(projectedIterator);
    }

    private void CreateDistinctRowsSet(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        if (querySpecificationNode.DistinctNode == null || querySpecificationNode.DistinctNode.IsEmpty)
        {
            return;
        }

        ResolveNodesTypes(querySpecificationNode.DistinctNode, context);
        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, context);
        var funcUnits = querySpecificationNode.DistinctNode.
            On.Select(d => makeDelegateVisitor.RunAndReturn(d)).ToArray();
        context.SetIterator(new DistinctRowsIterator(context.CurrentIterator, funcUnits));
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
        CreatePrefetchProjection(context, new List<IAstNode> { selectTableExpressionNode });

        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, context);
        var predicate = makeDelegateVisitor.RunAndReturn(selectTableExpressionNode.SearchConditionNode);
        context.SetIterator(new FilterRowsIterator(context.CurrentIterator, predicate));
    }

    #endregion

    #region INTO

    private void CreateOutput(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        context.HasFinalRowsIterator = true;

        var queryContext = new RowsOutputQueryContext(context.CurrentIterator.Columns);
        if (querySpecificationNode.Target == null)
        {
            return;
        }

        ResolveNodesTypes(querySpecificationNode.Target, context);
        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, context);
        var func = makeDelegateVisitor.RunAndReturn(querySpecificationNode.Target);
        var functionCallInfo = querySpecificationNode.Target
            .GetRequiredAttribute<FunctionCallInfo>(AstAttributeKeys.ArgumentsKey);
        var hasVaryingTarget = querySpecificationNode.Target.Arguments.Count > 0;
        var outputIterator = new VaryingOutputRowsIterator(
            context.CurrentIterator,
            func,
            functionCallInfo,
            ExecutionThread.Options.DefaultRowsOutput,
            queryContext);

        // If we have INTO clause defined we execute iterator and write rows into
        // INTO function rows output. Otherwise we just return IRowsIterator as is and
        // executor writes it into default output.
        if (outputIterator.HasOutputDefined)
        {
            context.HasOutput = true;
            var resultIterator = !hasVaryingTarget
                ? new AdjustColumnsLengthsIterator(outputIterator)
                : (IRowsIterator)outputIterator;
            var actionIterator = new ActionRowsIterator(resultIterator, "write to output")
            {
                BeforeMoveNext = _ =>
                {
                    while (outputIterator.MoveNext())
                    {
                        outputIterator.CurrentOutput.Write(resultIterator.Current);
                    }
                },
            };
            context.SetIterator(actionIterator);
        }
    }

    #endregion

    private void CreatePrefetchProjection(SelectCommandContext context, IEnumerable<IAstNode?> nodes)
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
        context.SetIterator(fetchIterator);
    }

    private ISet<int> GetColumnsIdsFromNode(IRowsIterator rowsIterator, IEnumerable<IAstNode?> nodes)
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
