using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Indexes;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// The visitor creates <see cref="IRowsIterator" /> as result.
/// </summary>
internal sealed partial class SelectQueryBodyVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;

    public SelectQueryBodyVisitor(ExecutionThread executionThread)
    {
        this._executionThread = executionThread;
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        var traversal = new AstTraversal(this);
        traversal.PostOrder(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQueryExpressionBodyNode node)
    {
        var combineRowsIterator = new CombineRowsIterator();
        foreach (var queryNode in node.Queries)
        {
            var queryContext = queryNode.GetAttribute<SelectCommandContext>(Constants.ResultKey);
            if (queryContext == null)
            {
                throw new InvalidOperationException("Invalid argument.");
            }
            combineRowsIterator.AddRowsIterator(queryContext.CurrentIterator);
        }

        var resultIterator = combineRowsIterator.RowIterators.Count == 1
            ? combineRowsIterator.RowIterators.First()
            : combineRowsIterator;
        if (_executionThread.Options.AddRowNumberColumn)
        {
            resultIterator = new RowIdRowsIterator(resultIterator);
        }

        node.SetAttribute(Constants.ResultKey, resultIterator);
        node.SetFunc(() => VariantValue.CreateFromObject(resultIterator));
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        // FROM.
        var context = CreateSourceRowsSet(node);

        ApplyStatistic(context);
        ResolveSelectAllStatement(context.CurrentIterator, node.ColumnsList);
        ResolveNodesTypes(node, context.CurrentIterator);

        // WHERE.
        ApplyFilter(context, node.TableExpression);

        CreatePrefetchProjection(context,
            node.GetChildren()
                .Except(new[] { node.TableExpression?.SearchConditionNode })
                .ToArray());

        // GROUP BY.
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

        node.SetAttribute(Constants.ResultKey, context);
    }

    #region FROM

    private IReadOnlyList<SelectInputQueryContext> OpenInputSources(SelectTableReferenceListNode fromNode)
    {
        var contexts = new List<SelectInputQueryContext>();
        var makeDelegateVisitor = new MakeDelegateVisitor(_executionThread);
        foreach (var fromTableExpression in fromNode.TableFunctions)
        {
            if (fromTableExpression is SelectTableFunctionNode fromTableFunction)
            {
                var source = makeDelegateVisitor.RunAndReturn(fromTableFunction.TableFunction).Invoke();
                var type = source.GetInternalType();
                if (DataTypeUtils.IsSimple(type))
                {
                    fromTableFunction.SetAttribute(Constants.RowsInputKey, source);
                    continue;
                }
                var rowsInput = source.AsObject as IRowsInput;
                if (rowsInput == null)
                {
                    if (source.AsObject is IRowsIterator rowsIterator)
                    {
                        fromTableFunction.SetAttribute(Constants.RowsInputKey, rowsIterator);
                    }
                    else
                    {
                        Logger.Instance.Warning("Invalid rows input type!");
                    }
                    continue;
                }
                fromTableFunction.SetAttribute(Constants.RowsInputKey, rowsInput);

                var context = new SelectInputQueryContext(rowsInput);
                contexts.Add(context);
                rowsInput.Open();
                rowsInput.SetContext(context);

                if (!string.IsNullOrEmpty(fromTableFunction.Alias))
                {
                    foreach (var inputColumn in rowsInput.Columns)
                    {
                        inputColumn.SourceName = fromTableFunction.Alias;
                    }
                }

                Logger.Instance.Debug($"Open rows input {rowsInput}.", nameof(SelectQueryBodyVisitor));
            }
            else if (fromTableExpression is SelectQueryExpressionBodyNode queryExpressionBodyNode)
            {
                var iterator = fromTableExpression.GetAttribute<IRowsIterator>(Constants.ResultKey);
                queryExpressionBodyNode.SetAttribute(Constants.RowsInputKey, iterator);
            }
        }
        return contexts;
    }

    private void ApplyStatistic(SelectCommandContext context)
    {
        context.AppendIterator(new CountRowsIterator(context.CurrentIterator, _executionThread.Statistic));
    }

    private SelectCommandContext CreateSourceRowsSet(SelectQuerySpecificationNode querySpecificationNode)
    {
        // No FROM - assumed this is the query with SELECT only.
        if (querySpecificationNode.TableExpression == null)
        {
            return new(new SingleValueRowsInput().AsIterable());
        }

        new ResolveTypesVisitor(_executionThread).Run(querySpecificationNode.TableExpression.Tables);

        // By opening input source we resolve all columns.
        var inputContexts = OpenInputSources(querySpecificationNode.TableExpression.Tables);

        // Start with FROM statement, if none - there is only one SELECT row.
        var rowsInputs = new List<IRowsInput>();
        IRowsIterator? resultRowsIterator = null;
        foreach (var tableFunction in querySpecificationNode.TableExpression.Tables.TableFunctions)
        {
            var rowsInput = tableFunction.GetAttribute(Constants.RowsInputKey) as IRowsInput;
            if (rowsInput == null && tableFunction.GetAttribute(Constants.RowsInputKey) is IRowsIterator rowsIterator)
            {
                rowsInput = new RowsIteratorInput(rowsIterator);
            }
            if (rowsInput == null && tableFunction.GetAttribute(Constants.RowsInputKey) is VariantValue value)
            {
                rowsInput = new SingleValueRowsInput(value);
            }
            if (rowsInput == null)
            {
                throw new SemanticException(Resources.Errors.InvalidRowsInputType);
            }
            rowsInputs.Add(rowsInput);
        }
        if (rowsInputs.Count == 1)
        {
            resultRowsIterator = new RowsInputIterator(rowsInputs[0], autoFetch: false);
        }

        // If more than one source on FROM clause, create multiplication.
        if (rowsInputs.Count > 1)
        {
            var multipleIterator = new MultiplyRowsIterator(
                new RowsInputIterator(rowsInputs[0], autoFetch: true),
                new RowsInputIterator(rowsInputs[1], autoFetch: true));
            for (int i = 2; i < rowsInputs.Count; i++)
            {
                multipleIterator = new MultiplyRowsIterator(
                    multipleIterator, new RowsInputIterator(rowsInputs[i], autoFetch: true));
            }
            resultRowsIterator = multipleIterator;
        }

        if (resultRowsIterator == null)
        {
            throw new QueryCatException("No input sources defined.");
        }

        var tearDownIterator = new TearDownRowsIterator(resultRowsIterator, "close inputs")
        {
            Action = _ =>
            {
                foreach (var rowsInput in rowsInputs)
                {
                    (rowsInput as IDisposable)?.Dispose();
                }
            }
        };
        var context = new SelectCommandContext(tearDownIterator)
        {
            // TODO: this will cause problems for MultiplyRowsIterator.
            RowsInputIterator = resultRowsIterator as RowsInputIterator,
            InputQueryContextList = inputContexts.ToArray(),
        };
        return context;
    }

    #endregion

    #region SELECT

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

        var makeDelegateVisitor = new SelectMakeDelegateVisitor(_executionThread, context);
        var projectedIterator = new ProjectedRowsIterator(context.CurrentIterator, context.ColumnsInfoContainer);
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

        for (int i = 0; i < node.Columns.Count; i++)
        {
            var columnNode = node.Columns[i];
            var columnName = GetColumnName(columnNode);
            if (string.IsNullOrEmpty(columnName) && node.Columns.Count == 1)
            {
                columnName = SingleValueRowsIterator.ColumnTitle;
            }
            var column = !string.IsNullOrEmpty(columnName)
                ? new Column(columnName, columnNode.GetDataType())
                : new Column(i + 1, columnNode.GetDataType());

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

        CreatePrefetchProjection(context, selectTableExpressionNode);

        var makeDelegateVisitor = new SelectMakeDelegateVisitor(_executionThread, context);
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

        // Create wrapper to initialize rows frame and create index.
        var makeDelegateVisitor = new SelectMakeDelegateVisitor(_executionThread, context);
        var orderFunctions = orderByNode.OrderBySpecificationNodes.Select(n =>
            new OrderRowsIterator.OrderBy(
                makeDelegateVisitor.RunAndReturn(n.Expression),
                ConvertDirection(n.Order)
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
        var makeDelegateVisitor = new SelectMakeDelegateVisitor(_executionThread, context);
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
        if (querySpecificationNode.Target != null)
        {
            var makeDelegateVisitor = new SelectMakeDelegateVisitor(_executionThread, context);
            var func = makeDelegateVisitor.RunAndReturn(querySpecificationNode.Target);
            var functionCallInfo = querySpecificationNode.Target.GetAttribute<FunctionCallInfo>(Constants.ArgumentsKey)
                ?? throw new InvalidOperationException("FunctionCallInfo is not set on function node.");
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
            context.CurrentIterator = new ActionRowsIterator(outputIterator, "write to output")
            {
                BeforeMoveNext = _ =>
                {
                    while (outputIterator.MoveNext())
                    {
                        outputIterator.CurrentOutput.Write(outputIterator.Current);
                    }
                }
            };
        }
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
        var projectedIterator = new ProjectedRowsIterator(context.CurrentIterator, context.ColumnsInfoContainer);
        context.AppendIterator(projectedIterator);

        foreach (var columnIndex in inputColumnIndexesForSelect)
        {
            projectedIterator.AddFuncColumn(
                context.RowsInputIterator.Columns[columnIndex],
                new FuncUnit(data => data.RowsIterator.Current[columnIndex]));
        }
    }

    private void ResolveNodesTypes(IAstNode? node, IRowsIterator rowsIterator)
    {
        if (node == null)
        {
            return;
        }
        new SelectResolveTypesVisitor(_executionThread, rowsIterator).Run(node);
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
