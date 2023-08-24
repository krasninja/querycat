using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Indexes;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    private const string SourceInputColumn = "source_input_column";

    #region SELECT

    private static void Pipeline_ResolveSelectAllStatement(IRowsIterator rows, SelectColumnsListNode columnsNode)
    {
        for (int i = 0; i < columnsNode.ColumnsNodes.Count; i++)
        {
            if (columnsNode.ColumnsNodes[i] is not SelectColumnsSublistAll)
            {
                continue;
            }

            columnsNode.ColumnsNodes.Remove(columnsNode.ColumnsNodes[i]);
            for (var columnIndex = 0; columnIndex < rows.Columns.Length; columnIndex++)
            {
                var column = rows.Columns[columnIndex];

                var astColumn = new SelectColumnsSublistExpressionNode(
                    new IdentifierExpressionNode(column.Name, column.SourceName));
                columnsNode.ColumnsNodes.Insert(i + columnIndex, astColumn);
            }
        }
    }

    private void Pipeline_CreateDistinctOnRowsSet(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        if (querySpecificationNode.DistinctNode == null || querySpecificationNode.DistinctNode.IsEmpty
            || !querySpecificationNode.DistinctNode.OnNodes.Any())
        {
            return;
        }

        var funcUnits = Misc_CreateDelegate(querySpecificationNode.DistinctNode.OnNodes, context);
        context.SetIterator(new DistinctRowsIteratorIterator(context.CurrentIterator, funcUnits));
    }

    private void Pipeline_CreateDistinctAllRowsSet(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        if (querySpecificationNode.DistinctNode == null || querySpecificationNode.DistinctNode.IsEmpty
            || querySpecificationNode.DistinctNode.OnNodes.Any())
        {
            return;
        }

        var funcUnits = context.CurrentIterator.Columns
            .Select((_, i) => new FuncUnitRowsIteratorColumn(context.CurrentIterator, i))
            .Cast<IFuncUnit>()
            .ToArray();
        context.SetIterator(new DistinctRowsIteratorIterator(context.CurrentIterator, funcUnits));
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
            return string.Empty;
        }

        string GetColumnSourceName(SelectColumnsSublistNode columnNode)
        {
            if (columnNode is SelectColumnsSublistExpressionNode expressionNode
                && expressionNode.ExpressionNode is IdentifierExpressionNode identifierExpressionNode)
            {
                return identifierExpressionNode.SourceName;
            }
            return string.Empty;
        }

        for (var i = 0; i < node.ColumnsNodes.Count; i++)
        {
            var columnNode = node.ColumnsNodes[i];
            var columnName = GetColumnName(columnNode);
            // Do not set source name if alias is specified.
            var columnSourceName = string.IsNullOrEmpty(columnNode.Alias)
                ? GetColumnSourceName(columnNode)
                : string.Empty;
            if (string.IsNullOrEmpty(columnName) && node.ColumnsNodes.Count == 1)
            {
                columnName = SingleValueRowsIterator.ColumnTitle;
            }
            var column = !string.IsNullOrEmpty(columnName)
                ? new Column(columnName, columnSourceName, columnNode.GetDataType())
                : new Column(i + 1, columnNode.GetDataType());

            var sourceInputColumn = columnNode.GetAttribute<Column>(SourceInputColumn);
            if (sourceInputColumn != null)
            {
                column.Description = sourceInputColumn.Description;
            }

            yield return new ColumnWithIndex(column, i);
        }
    }

    private void Pipeline_AddSelectRowsSet(
        SelectCommandContext context,
        SelectColumnsListNode columnsNode)
    {
        var projectedIterator = new ProjectedRowsIterator(context.CurrentIterator);

        var funcs = columnsNode.ColumnsNodes.Select(c => Misc_CreateDelegate(c, context)).ToList();
        var selectColumns = CreateSelectColumns(columnsNode).ToList();
        for (var i = 0; i < columnsNode.ColumnsNodes.Count; i++)
        {
            var columnIndex = projectedIterator.AddFuncColumn(selectColumns[i].Column, funcs[i]);
            var info = context.ColumnsInfoContainer.GetByColumn(projectedIterator.Columns[columnIndex]);
            info.RelatedSelectSublistNode = columnsNode.ColumnsNodes[selectColumns[i].ColumnIndex];
        }

        var iterator = context.CurrentIterator;
        for (var i = 0; i < iterator.Columns.Length; i++)
        {
            var column = iterator.Columns[i];
            if (projectedIterator.GetColumnIndexByName(column.Name, column.SourceName) == -1)
            {
                projectedIterator.AddFuncColumn(
                    column, new FuncUnitRowsIteratorColumn(iterator, i));
            }
        }

        context.SetIterator(projectedIterator);
    }

    private static void Pipeline_SetSelectRowsSet(
        SelectCommandContext context,
        SelectColumnsListNode columnsNode)
    {
        var projectedIterator = new ProjectedRowsIterator(context.CurrentIterator);
        var iterator = context.CurrentIterator;
        foreach (var selectColumn in columnsNode.ColumnsNodes)
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

    #endregion

    #region WHERE

    private void Pipeline_ApplyFilter(SelectCommandContext context, SelectTableExpressionNode? selectTableExpressionNode)
    {
        if (selectTableExpressionNode?.SearchConditionNode == null)
        {
            return;
        }

        var predicate = Misc_CreateDelegate(selectTableExpressionNode.SearchConditionNode, context);
        context.SetIterator(new FilterRowsIterator(context.CurrentIterator, predicate));
    }

    #endregion

    #region ORDER BY

    private void Pipeline_ApplyOrderBy(SelectCommandContext context, SelectOrderByNode? orderByNode)
    {
        if (orderByNode == null)
        {
            return;
        }

        // Create wrapper to initialize rows frame and create index.
        var orderFunctions = orderByNode.OrderBySpecificationNodes.Select(n =>
            new OrderByData(
                Misc_CreateDelegate(n.ExpressionNode, context),
                Pipeline_ConvertDirection(n.Order),
                Pipeline_ConvertNullOrder(n.NullOrder)
            )
        );
        context.SetIterator(new OrderRowsIterator(context.CurrentIterator, orderFunctions.ToArray()));
    }

    private static OrderDirection Pipeline_ConvertDirection(SelectOrderSpecification order) => order switch
    {
        SelectOrderSpecification.Ascending => OrderDirection.Ascending,
        SelectOrderSpecification.Descending => OrderDirection.Descending,
        _ => throw new ArgumentOutOfRangeException(nameof(order)),
    };

    private static NullOrder Pipeline_ConvertNullOrder(SelectNullOrder order) => order switch
    {
        SelectNullOrder.NullsFirst => NullOrder.NullsFirst,
        SelectNullOrder.NullsLast => NullOrder.NullsLast,
        _ => throw new ArgumentOutOfRangeException(nameof(order)),
    };

    #endregion

    #region OFFSET, FETCH

    private void Pipeline_ApplyOffsetFetch(
        SelectCommandContext context,
        SelectOffsetNode? offsetNode,
        SelectFetchNode? fetchNode)
    {
        if (offsetNode != null)
        {
            var count = Misc_CreateDelegate(offsetNode.CountNode, context).Invoke().AsInteger;
            context.SetIterator(new OffsetRowsIterator(context.CurrentIterator, count));
        }
        if (fetchNode != null)
        {
            var count = Misc_CreateDelegate(fetchNode.CountNode, context).Invoke().AsInteger;
            context.SetIterator(new LimitRowsIterator(context.CurrentIterator, count));
        }
    }

    #endregion

    #region INTO

    private void Pipeline_SetOutputFunction(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        if (querySpecificationNode.TargetNode == null)
        {
            return;
        }

        var func = Misc_CreateDelegate(querySpecificationNode.TargetNode, context);
        context.OutputArgumentsFunc = func;
    }

    private void Pipeline_CreateOutput(
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
            var resultIterator = !hasVaryingTarget && ExecutionThread.Options.AnalyzeRowsCount > 0
                ? new AdjustColumnsLengthsIterator(outputIterator, ExecutionThread.Options.AnalyzeRowsCount)
                : (IRowsIterator)outputIterator;
            var actionIterator = new ActionRowsIterator(resultIterator, "write to output")
            {
                BeforeMoveNext = _ =>
                {
                    while (outputIterator.MoveNext())
                    {
                        outputIterator.CurrentOutput.WriteValues(resultIterator.Current.Values);
                    }
                },
            };
            context.SetIterator(actionIterator);
        }
    }

    #endregion

    #region Misc

    private void Pipeline_ApplyStatistic(SelectCommandContext context)
    {
        context.SetIterator(new StatisticRowsIterator(context.CurrentIterator, ExecutionThread.Statistic)
        {
            MaxErrorsCount = ExecutionThread.Options.MaxErrors,
        });
    }

    /// <summary>
    /// Update statistic if there is a error in rows input.
    /// </summary>
    private void Pipeline_SubscribeOnErrorsFromInputSources(SelectCommandContext context)
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

    /// <summary>
    /// Assign SourceInputColumn attribute based on rows input iterator.
    /// </summary>
    private static void Pipeline_ResolveSelectSourceColumns(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        if (context.RowsInputIterator == null)
        {
            return;
        }

        foreach (var column in querySpecificationNode.ColumnsListNode.ColumnsNodes.OfType<SelectColumnsSublistExpressionNode>())
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

    #endregion
}
