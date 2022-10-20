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
        var context = CreateSourceContext(node);

        ApplyStatistic(context);
        SubscribeOnErrorsFromInputSources(context);
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

    /// <summary>
    /// Create and open input source.
    /// This method should set <see cref="AstAttributeKeys.RowsInputKey" /> key.
    /// </summary>
    private IRowsInput CreateInputSourceFromFunction(
        SelectTableFunctionNode fromTableFunction,
        IList<SelectInputQueryContext> queryContexts)
    {
        var makeDelegateVisitor = new MakeDelegateVisitor(_executionThread);

        var source = makeDelegateVisitor.RunAndReturn(fromTableFunction.TableFunction).Invoke();

        if (DataTypeUtils.IsSimple(source.GetInternalType()))
        {
            return new SingleValueRowsInput(source);
        }
        if (source.AsObject is IRowsInput)
        {
            var rowsInput = (IRowsInput)source.AsObject;
            rowsInput.Open();
            Logger.Instance.Debug($"Open rows input {rowsInput}.", nameof(SelectQueryBodyVisitor));
            var context = new SelectInputQueryContext(rowsInput);
            rowsInput.SetContext(context);
            queryContexts.Add(context);
            return rowsInput;
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            return new RowsIteratorInput(rowsIterator);
        }

        throw new QueryCatException("Invalid rows input.");
    }

    private void ApplyStatistic(SelectCommandContext context)
    {
        context.AppendIterator(new StatisticRowsIterator(context.CurrentIterator, _executionThread.Statistic)
        {
            MaxErrorsCount = _executionThread.Options.MaxErrors,
        });
    }

    private SelectCommandContext CreateSourceContext(SelectQuerySpecificationNode querySpecificationNode)
    {
        IRowsIterator CreateMultipleIterator(List<IRowsInput> rowsInputs)
        {
            if (rowsInputs.Count == 0)
            {
                throw new QueryCatException("No rows inputs.");
            }
            if (rowsInputs.Count == 1)
            {
                return new RowsInputIterator(rowsInputs[0], autoFetch: false);
            }
            var multipleIterator = new MultiplyRowsIterator(
                new RowsInputIterator(rowsInputs[0], autoFetch: true),
                new RowsInputIterator(rowsInputs[1], autoFetch: true));
            for (int i = 2; i < rowsInputs.Count; i++)
            {
                multipleIterator = new MultiplyRowsIterator(
                    multipleIterator, new RowsInputIterator(rowsInputs[i], autoFetch: true));
            }
            return multipleIterator;
        }

        void DisposeRowsInputs(List<IRowsInput> rowsInputs)
        {
            foreach (var rowsInput in rowsInputs)
            {
                (rowsInput as IDisposable)?.Dispose();
            }
        }

        IRowsInput CreateInputSourceFromSubQuery(SelectQueryExpressionBodyNode queryExpressionBodyNode)
        {
            var iterator = queryExpressionBodyNode.GetAttribute<IRowsIterator>(AstAttributeKeys.ResultKey);
            if (iterator == null)
            {
                throw new QueryCatException("No iterator for subquery!");
            }
            return new RowsIteratorInput(iterator);
        }

        // Entry point here.
        //

        // No FROM - assumed this is the query with SELECT only.
        if (querySpecificationNode.TableExpression == null)
        {
            return new(new SingleValueRowsInput().AsIterable());
        }

        // Start with FROM statement, if none - there is only one SELECT row.
        var rowsInputs = new List<IRowsInput>();
        var inputContexts = new List<SelectInputQueryContext>();
        foreach (var tableExpression in querySpecificationNode.TableExpression.Tables.TableFunctions)
        {
            IRowsInput rowsInput;
            string alias;
            if (tableExpression is SelectQueryExpressionBodyNode selectQueryExpressionBodyNode)
            {
                rowsInput = CreateInputSourceFromSubQuery(selectQueryExpressionBodyNode);
                alias = selectQueryExpressionBodyNode.Alias;
            }
            else if (tableExpression is SelectTableFunctionNode tableFunctionNode)
            {
                new ResolveTypesVisitor(_executionThread).Run(tableExpression);
                rowsInput = CreateInputSourceFromFunction(tableFunctionNode, inputContexts);
                alias = tableFunctionNode.Alias;
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(Resources.Errors.CannotProcessNodeAsInput, tableExpression));
            }

            SetAlias(tableExpression, alias);
            SetAlias(rowsInput, alias);
            tableExpression.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
            rowsInputs.Add(rowsInput);
        }

        var resultRowsIterator = CreateMultipleIterator(rowsInputs);
        var tearDownIterator = new TearDownRowsIterator(resultRowsIterator, "close inputs")
        {
            Action = _ => DisposeRowsInputs(rowsInputs)
        };
        return new SelectCommandContext(tearDownIterator)
        {
            // TODO: this will cause problems for MultiplyRowsIterator.
            RowsInputIterator = resultRowsIterator as RowsInputIterator,
            InputQueryContextList = inputContexts.ToArray(),
        };
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

    private void SetAlias(IAstNode node, string alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return;
        }
        foreach (var inputColumn in node.GetAllChildren<IdentifierExpressionNode>()
                     .Where(n => string.IsNullOrEmpty(n.SourceName)))
        {
            inputColumn.SourceName = alias;
        }
        foreach (var inputColumn in node.GetAllChildren<SelectColumnsSublistNameNode>()
                     .Where(n => string.IsNullOrEmpty(n.SourceName)))
        {
            inputColumn.SourceName = alias;
        }
    }

    private void SetAlias(IRowsInput input, string alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return;
        }
        foreach (var column in input.Columns.Where(c => string.IsNullOrEmpty(c.SourceName)))
        {
            column.SourceName = alias;
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
