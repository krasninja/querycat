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
    private const string SourceInputColumn = "source_input_column";

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
            var queryContext = queryNode.GetAttribute<SelectCommandContext>(AstAttributeKeys.ResultKey);
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

        node.SetAttribute(AstAttributeKeys.ResultKey, resultIterator);
        node.SetFunc(() => VariantValue.CreateFromObject(resultIterator));
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        // FROM.
        var context = CreateSourceRowsSet(node);

        ApplyStatistic(context);
        ProcessErrorsFromInputSources(context);
        ResolveSelectAllStatement(context.CurrentIterator, node.ColumnsList);
        ResolveSelectSourceColumns(context, node);
        ResolveNodesTypes(node, context.CurrentIterator);
        FillQueryContextConditions(node, context);

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

        node.SetAttribute(AstAttributeKeys.ResultKey, context);
    }

    #region FROM

    private void ProcessErrorsFromInputSources(SelectCommandContext context)
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
                    fromTableFunction.SetAttribute(AstAttributeKeys.RowsInputKey, source);
                    continue;
                }
                var rowsInput = source.AsObject as IRowsInput;
                if (rowsInput == null)
                {
                    if (source.AsObject is IRowsIterator rowsIterator)
                    {
                        fromTableFunction.SetAttribute(AstAttributeKeys.RowsInputKey, rowsIterator);
                    }
                    else
                    {
                        Logger.Instance.Warning("Invalid rows input type!");
                    }
                    continue;
                }
                fromTableFunction.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);

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
                var iterator = fromTableExpression.GetAttribute<IRowsIterator>(AstAttributeKeys.ResultKey);
                queryExpressionBodyNode.SetAttribute(AstAttributeKeys.RowsInputKey, iterator);

                if (!string.IsNullOrEmpty(queryExpressionBodyNode.Alias))
                {
                    foreach (var inputColumn in queryExpressionBodyNode.GetAllChildren<IdentifierExpressionNode>())
                    {
                        inputColumn.SourceName = queryExpressionBodyNode.Alias;
                    }
                    foreach (var inputColumn in queryExpressionBodyNode.GetAllChildren<SelectColumnsSublistNameNode>())
                    {
                        inputColumn.SourceName = queryExpressionBodyNode.Alias;
                    }

                    var rowsIterator = queryExpressionBodyNode.GetAttribute<IRowsIterator>(AstAttributeKeys.RowsInputKey);
                    if (rowsIterator != null)
                    {
                        foreach (var column in rowsIterator.Columns)
                        {
                            column.SourceName = queryExpressionBodyNode.Alias;
                        }
                    }
                }
            }
        }
        return contexts;
    }

    private void ApplyStatistic(SelectCommandContext context)
    {
        context.AppendIterator(new StatisticRowsIterator(context.CurrentIterator, _executionThread.Statistic)
        {
            MaxErrorsCount = _executionThread.Options.MaxErrors,
        });
    }

    private SelectCommandContext CreateSourceRowsSet(SelectQuerySpecificationNode querySpecificationNode)
    {
        // No FROM - assumed this is the query with SELECT only.
        if (querySpecificationNode.TableExpression == null)
        {
            return new(new SingleValueRowsInput().AsIterable());
        }

        foreach (var tableExpression in querySpecificationNode.TableExpression.Tables.TableFunctions)
        {
            if (tableExpression is SelectQueryExpressionBodyNode)
            {
                continue;
            }
            new ResolveTypesVisitor(_executionThread).Run(querySpecificationNode.TableExpression.Tables);
        }

        // By opening input source we resolve all columns.
        var inputContexts = OpenInputSources(querySpecificationNode.TableExpression.Tables);

        // Start with FROM statement, if none - there is only one SELECT row.
        var rowsInputs = new List<IRowsInput>();
        IRowsIterator? resultRowsIterator = null;
        foreach (var tableFunction in querySpecificationNode.TableExpression.Tables.TableFunctions)
        {
            var rowsInput = tableFunction.GetAttribute(AstAttributeKeys.RowsInputKey) as IRowsInput;
            if (rowsInput == null && tableFunction.GetAttribute(AstAttributeKeys.RowsInputKey) is IRowsIterator rowsIterator)
            {
                rowsInput = new RowsIteratorInput(rowsIterator);
            }
            if (rowsInput == null && tableFunction.GetAttribute(AstAttributeKeys.RowsInputKey) is VariantValue value)
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

    private void FillQueryContextConditions(
        SelectQuerySpecificationNode querySpecificationNode,
        SelectCommandContext commandContext)
    {
        foreach (var context in commandContext.InputQueryContextList)
        {
            FillQueryContextConditions(querySpecificationNode, context, commandContext);
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
            var makeDelegateVisitor = new SelectMakeDelegateVisitor(_executionThread, commandContext);
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
            var makeDelegateVisitor = new SelectMakeDelegateVisitor(_executionThread, commandContext);
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

    private void ResolveSelectSourceColumns(SelectCommandContext context,
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
            var functionCallInfo = querySpecificationNode.Target.GetAttribute<FunctionCallInfo>(AstAttributeKeys.ArgumentsKey)
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
