using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// The visitor creates <see cref="IRowsIterator" /> as result for <see cref="SelectQuerySpecificationNode" /> node.
/// </summary>
internal sealed partial class SpecificationNodeVisitor : SelectAstVisitor
{
    private const string SourceInputColumn = "source_input_column";

    public SpecificationNodeVisitor(ExecutionThread executionThread) : base(executionThread)
    {
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        AstTraversal.PostOrder(node);
    }

    #region MAIN

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
        ResolveSelectAllStatement(context.CurrentIterator, node.ColumnsListNode);
        ResolveSelectSourceColumns(context, node);

        // WHERE.
        ApplyFilter(context, node.TableExpressionNode);

        // Fetch remain data.
        CreatePrefetchProjection(context, new List<IAstNode?>
        {
            node.ColumnsListNode, node.TargetNode, node.DistinctNode, node.OrderByNode, node.OffsetNode, node.FetchNode,
            node.TableExpressionNode
        });

        // GROUP BY/HAVING.
        ApplyAggregate(context, node);
        ApplyHaving(context, node.TableExpressionNode?.HavingNode);

        // SELECT.
        AddSelectRowsSet(context, node.ColumnsListNode);
        FillQueryContextConditions(node, context);

        // DISTINCT.
        CreateDistinctRowsSet(context, node);

        // ORDER BY.
        ApplyOrderBy(context, node.OrderByNode);

        // INTO and SELECT.
        SetOutputFunction(context, node);
        SetSelectRowsSet(context, node.ColumnsListNode);

        // OFFSET, FETCH.
        ApplyOffsetFetch(context, node.OffsetNode, node.FetchNode);

        // INTO.
        CreateOutput(context, node);
    }

    #endregion

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
        context.RowsInputIterator.OnError += (_, args) =>
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

    #endregion

    #region SELECT

    /// <summary>
    /// Assign SourceInputColumn attribute based on rows input iterator.
    /// </summary>
    private static void ResolveSelectSourceColumns(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        if (context.RowsInputIterator == null)
        {
            return;
        }

        foreach (var column in querySpecificationNode.ColumnsListNode.Columns.OfType<SelectColumnsSublistExpressionNode>())
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

    private static void ResolveSelectAllStatement(IRowsIterator rows, SelectColumnsListNode columnsNode)
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
            projectedIterator.AddFuncColumn(
                context.CurrentIterator.Columns[i], new FuncUnitRowsIteratorColumn(iterator, i));
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

    private static void SetSelectRowsSet(
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
                projectedIterator.AddFuncColumn(columnInfo.Column, new FuncUnitRowsIteratorColumn(iterator, index));
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

    private static IEnumerable<ColumnWithIndex> CreateSelectColumns(SelectColumnsListNode node)
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
            // Do not set source name if alias is specified.
            var columnSourceName = string.IsNullOrEmpty(columnNode.Alias)
                ? GetColumnSourceName(columnNode)
                : string.Empty;
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

    private void SetOutputFunction(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        if (querySpecificationNode.TargetNode == null)
        {
            return;
        }

        ResolveNodesTypes(querySpecificationNode.TargetNode, context);
        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, context);
        var func = makeDelegateVisitor.RunAndReturn(querySpecificationNode.TargetNode);
        context.OutputArgumentsFunc = func;
    }

    private void CreateOutput(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        context.HasFinalRowsIterator = true;
        if (querySpecificationNode.TargetNode == null
            || context.OutputArgumentsFunc == null)
        {
            return;
        }

        var queryContext = new RowsOutputQueryContext(context.CurrentIterator.Columns);
        var functionCallInfo = querySpecificationNode.TargetNode
            .GetRequiredAttribute<FunctionCallInfo>(AstAttributeKeys.ArgumentsKey);
        var hasVaryingTarget = querySpecificationNode.TargetNode.Arguments.Count > 0;
        var outputIterator = new VaryingOutputRowsIterator(
            context.CurrentIterator,
            context.OutputArgumentsFunc,
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

        var inputColumnIndexesForSelect = GetColumnsIdsFromNode(context.RowsInputIterator, nodes)
            .ToArray();
        if (inputColumnIndexesForSelect.Length < 1
            || context.PrefetchedColumnIndexes.SetEquals(inputColumnIndexesForSelect))
        {
            return;
        }

        foreach (var index in inputColumnIndexesForSelect)
        {
            context.PrefetchedColumnIndexes.Add(index);
        }
        var fetchIterator = new PrefetchRowsIterator(context.CurrentIterator, context.RowsInputIterator,
            inputColumnIndexesForSelect);
        context.SetIterator(fetchIterator);
    }

    private static ISet<int> GetColumnsIdsFromNode(IRowsIterator rowsIterator, IEnumerable<IAstNode?> nodes)
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
