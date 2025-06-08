using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Indexes;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    private const string SourceInputColumn = "source_input_column";

    #region SELECT

    private static void Pipeline_ResolveSelectAllStatement(SelectCommandContext context, SelectColumnsListNode columnsNode)
    {
        context.HasExactColumnsSelect = true;
        for (var i = 0; i < columnsNode.ColumnsNodes.Count; i++)
        {
            if (columnsNode.ColumnsNodes[i] is not SelectColumnsSublistAll selectColumnsSublistAll)
            {
                continue;
            }

            context.HasExactColumnsSelect = false;
            var iterator = context.CurrentIterator;
            columnsNode.ColumnsNodes.Remove(columnsNode.ColumnsNodes[i]);
            foreach (var column in iterator.Columns)
            {
                if (selectColumnsSublistAll.PrefixIdentifier != null &&
                    column.SourceName != selectColumnsSublistAll.PrefixIdentifier.TableFullName)
                {
                    continue;
                }

                var astColumn = new SelectColumnsSublistExpressionNode(
                    new IdentifierExpressionNode(column.Name, column.SourceName));
                columnsNode.ColumnsNodes.Add(astColumn);
            }
        }
    }

    private async Task Pipeline_CreateDistinctOnRowsSetAsync(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode,
        CancellationToken cancellationToken)
    {
        if (querySpecificationNode.DistinctNode == null || querySpecificationNode.DistinctNode.IsEmpty
            || !querySpecificationNode.DistinctNode.OnNodes.Any())
        {
            return;
        }

        var funcUnits = await Misc_CreateDelegateAsync(querySpecificationNode.DistinctNode.OnNodes, context, cancellationToken);
        context.SetIterator(new DistinctRowsIteratorIterator(ExecutionThread, context.CurrentIterator, funcUnits));
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
        context.SetIterator(new DistinctRowsIteratorIterator(ExecutionThread, context.CurrentIterator, funcUnits));
    }

    private readonly record struct ColumnWithIndex(
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
                return identifierExpressionNode.TableFieldName;
            }
            return string.Empty;
        }

        string GetColumnSourceName(SelectColumnsSublistNode columnNode)
        {
            if (columnNode is SelectColumnsSublistExpressionNode expressionNode
                && expressionNode.ExpressionNode is IdentifierExpressionNode identifierExpressionNode)
            {
                return identifierExpressionNode.TableSourceName;
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

    private async Task Pipeline_AddSelectRowsSetAsync(
        SelectCommandContext context,
        SelectColumnsListNode columnsNode,
        SelectColumnsExceptNode? exceptNode,
        CancellationToken cancellationToken)
    {
        var projectedIterator = new ProjectedRowsIterator(ExecutionThread, context.CurrentIterator);

        // Format the initial iterator with all columns (except excluded) that
        // user mentioned in SELECT block.
        var funcs = new List<IFuncUnit>();
        foreach (var node in columnsNode.ColumnsNodes)
        {
            funcs.Add(await Misc_CreateDelegateAsync(node, context, cancellationToken));
        }
        var selectColumns = CreateSelectColumns(columnsNode).ToList();
        var exceptColumns = exceptNode?.ExceptIdentifiers.ToList() ?? new List<IdentifierExpressionNode>();
        for (var i = 0; i < columnsNode.ColumnsNodes.Count; i++)
        {
            // Excluded columns filter.
            var columnNode = columnsNode.ColumnsNodes[i] as SelectColumnsSublistExpressionNode;
            if (columnNode?.ExpressionNode is IdentifierExpressionNode columnIdNode)
            {
                var columnToExcept = exceptColumns.Find(
                    node => node.TableFieldName.Equals(columnIdNode.TableFieldName, StringComparison.InvariantCultureIgnoreCase)
                            && node.TableSourceName.Equals(columnIdNode.TableSourceName, StringComparison.InvariantCultureIgnoreCase));
                if (columnToExcept != null)
                {
                    exceptColumns.Remove(columnToExcept);
                    continue;
                }
            }
            var columnIndex = projectedIterator.AddFuncColumn(selectColumns[i].Column, funcs[i]);
            var info = context.ColumnsInfoContainer.GetByColumnOrAdd(projectedIterator.Columns[columnIndex]);
            info.RelatedSelectSublistNode = columnsNode.ColumnsNodes[selectColumns[i].ColumnIndex];
        }

        // Check that all "exclude" identifiers are used.
        if (exceptColumns.Count > 0)
        {
            var availableColumns = string.Join(", ", columnsNode.GetColumnsNames().Select(c => $"'{c}'"));
            throw new SemanticException(
                string.Format(Resources.Errors.InvalidExceptColumn, exceptColumns[0].TableFullName, availableColumns));
        }

        // Add missed columns (for example, virtual and exclude columns) so that are visible
        // for filtering/processing.
        var iterator = context.CurrentIterator;
        for (var i = 0; i < iterator.Columns.Length; i++)
        {
            var column = iterator.Columns[i];
            if (projectedIterator.GetColumnIndexByName(column.Name, column.SourceName) == -1)
            {
                projectedIterator.AddFuncColumn(column, new FuncUnitRowsIteratorColumn(iterator, i));
            }
        }

        context.SetIterator(projectedIterator);
    }

    private void Pipeline_SetSelectRowsSet(
        SelectCommandContext context,
        SelectColumnsListNode columnsNode)
    {
        var projectedIterator = new ProjectedRowsIterator(ExecutionThread, context.CurrentIterator);
        var iterator = context.CurrentIterator;
        foreach (var selectColumn in columnsNode.ColumnsNodes)
        {
            var columnInfo = context.ColumnsInfoContainer.Columns
                .FirstOrDefault(c => c.RelatedSelectSublistNode == selectColumn);
            if (columnInfo == null)
            {
                System.Diagnostics.Debug.Write("Columns info not found!");
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

    private async Task Pipeline_ApplyFilterAsync(SelectCommandContext context, SelectTableNode? selectTableExpressionNode,
        CancellationToken cancellationToken)
    {
        if (selectTableExpressionNode?.SearchConditionNode == null)
        {
            return;
        }

        var predicate = await Misc_CreateDelegateAsync(selectTableExpressionNode.SearchConditionNode, context, cancellationToken);
        context.SetIterator(new FilterRowsIterator(ExecutionThread, context.CurrentIterator, predicate));
    }

    #endregion

    #region ORDER BY

    private async Task Pipeline_ApplyOrderByAsync(SelectCommandContext context, SelectOrderByNode? orderByNode,
        CancellationToken cancellationToken)
    {
        if (orderByNode == null)
        {
            return;
        }

        Pipeline_OrderConvertColumnNumbers(context.CurrentIterator, orderByNode.OrderBySpecificationNodes);

        // Create wrapper to initialize rows frame and create index.
        var orderFunctions = new List<OrderByData>();
        foreach (var node in orderByNode.OrderBySpecificationNodes)
        {
            orderFunctions.Add(new OrderByData(
                await Misc_CreateDelegateAsync(node.ExpressionNode, context, cancellationToken),
                Pipeline_ConvertDirection(node.Order),
                Pipeline_ConvertNullOrder(node.NullOrder)
            ));
        }
        context.SetIterator(new OrderRowsIterator(ExecutionThread, context.CurrentIterator, orderFunctions.ToArray()));
    }

    private static void Pipeline_OrderConvertColumnNumbers(IRowsIterator currentIterator, List<SelectOrderBySpecificationNode> orderByNodes)
    {
        // Convert: SELECT id FROM x ORDER BY 1 -> SELECT id FROM x ORDER BY id.
        foreach (var orderByNode in orderByNodes)
        {
            if (orderByNode.ExpressionNode is LiteralNode literalNode
                && literalNode.Value.Type == DataType.Integer
                && literalNode.Value.AsIntegerUnsafe > 0
                && literalNode.Value.AsIntegerUnsafe <= currentIterator.Columns.Length)
            {
                var column = currentIterator.Columns[literalNode.Value.AsIntegerUnsafe - 1];
                orderByNode.ExpressionNode = new IdentifierExpressionNode(column.Name, column.SourceName);
            }
        }
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

    private async Task Pipeline_ApplyOffsetFetchAsync(
        SelectCommandContext context,
        SelectOffsetNode? offsetNode,
        SelectFetchNode? fetchNode,
        CancellationToken cancellationToken)
    {
        if (offsetNode != null)
        {
            var @delegate = await Misc_CreateDelegateAsync(offsetNode.CountNode, context, cancellationToken);
            var count = (await @delegate.InvokeAsync(ExecutionThread, cancellationToken)).AsInteger;
            if (count.HasValue)
            {
                context.SetIterator(new OffsetRowsIterator(context.CurrentIterator, count.Value));
            }
        }
        if (fetchNode != null)
        {
            var @delegate = await Misc_CreateDelegateAsync(fetchNode.CountNode, context, cancellationToken);
            var count = (await @delegate.InvokeAsync(ExecutionThread, cancellationToken)).AsInteger;
            if (count.HasValue)
            {
                context.SetIterator(new LimitRowsIterator(context.CurrentIterator, count.Value));
            }
        }
    }

    #endregion

    #region INTO

    private async Task Pipeline_SetOutputFunctionAsync(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode,
        CancellationToken cancellationToken)
    {
        if (querySpecificationNode.TargetNode == null)
        {
            return;
        }

        var func = await Misc_CreateDelegateAsync(querySpecificationNode.TargetNode, context, cancellationToken);
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

        var queryContext = new RowsOutputQueryContext(context.CurrentIterator.Columns, ExecutionThread.ConfigStorage);
        var functionCallInfo = querySpecificationNode.TargetNode
            .GetRequiredAttribute<FuncUnitCallInfo>(AstAttributeKeys.ArgumentsKey);
        var hasVaryingTarget = querySpecificationNode.TargetNode.Arguments.Count > 0;
        var outputIterator = new VaryingOutputRowsIterator(
            ExecutionThread,
            context.CurrentIterator,
            context.OutputArgumentsFunc,
            functionCallInfo,
            ExecutionThread.Options.DefaultRowsOutput,
            queryContext);

        // If we have INTO clause defined we execute iterator and write rows into
        // "INTO" function rows output. Otherwise, we just return IRowsIterator as is and
        // executor writes it into default output.
        if (outputIterator.HasOutputDefined)
        {
            context.HasOutput = true;
            if (!hasVaryingTarget && ExecutionThread.Options.AnalyzeRowsCount > 0)
            {
                context.SetIterator(
                    new AdjustColumnsLengthsIterator(outputIterator, ExecutionThread.Options.AnalyzeRowsCount));
            }
            else
            {
                var writeIterator = new OutputWriteRowsIterator(outputIterator, outputIterator.CurrentOutput);
                context.SetIterator(writeIterator);
            }
        }
    }

    #endregion

    #region Misc

    private void Pipeline_AddRowIdColumn(SelectCommandContext context, SelectColumnsListNode columnsNode)
    {
        var isSubQuery = context.Parent != null;
        var resultIterator = context.CurrentIterator;
        if (ExecutionThread.Options.AddRowNumberColumn
            && !isSubQuery
            && resultIterator.GetColumnIndexByName(RowIdRowsIterator.ColumName) == -1)
        {
            var rowIdIterator = new RowIdRowsIterator(resultIterator);
            resultIterator = rowIdIterator;

            if (!context.HasExactColumnsSelect)
            {
                var columnInfo = context.ColumnsInfoContainer.GetByColumnOrAdd(rowIdIterator.RowNumberColumn);
                var rowNumberExpressionNode = new SelectColumnsSublistExpressionNode(
                    new IdentifierExpressionNode(rowIdIterator.RowNumberColumn.Name));
                columnInfo.RelatedSelectSublistNode = rowNumberExpressionNode;
                columnsNode.ColumnsNodes.Insert(0, rowNumberExpressionNode);
            }
        }
        context.SetIterator(resultIterator);
    }

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
            if (ExecutionThread.Options.ShowDetailedStatistic)
            {
                ExecutionThread.Statistic.AddError(
                    new ExecutionStatistic.RowErrorInfo(args.ErrorCode, args.RowIndex, args.ColumnIndex));
            }
            else
            {
                ExecutionThread.Statistic.AddError(
                    new ExecutionStatistic.RowErrorInfo(args.ErrorCode));
            }
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
                var sourceColumn = context.RowsInputIterator.GetColumnByName(identifierExpressionNode.TableFieldName,
                    identifierExpressionNode.TableSourceName);
                if (sourceColumn != null)
                {
                    column.SetAttribute(SourceInputColumn, sourceColumn);
                }
            }
        }
    }

    #endregion
}
