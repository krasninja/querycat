using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed class SelectContextCreator
{
    private readonly ExecutionThread _executionThread;
    private readonly SelectCommandContext[] _parents;
    private readonly ResolveTypesVisitor _resolveTypesVisitor;
    private readonly Dictionary<IRowsInput, SelectInputQueryContext> _rowsInputContextMap = new();

    public SelectContextCreator(ExecutionThread executionThread, SelectCommandContext[]? parents = null)
    {
        this._executionThread = executionThread;
        this._resolveTypesVisitor = new ResolveTypesVisitor(executionThread);
        this._parents = parents ?? Array.Empty<SelectCommandContext>();
    }

    public IList<SelectCommandContext> CreateForQuery(IEnumerable<SelectQuerySpecificationNode> nodes)
    {
        return nodes.Select(CreateForQuery).ToList();
    }

    public SelectCommandContext CreateForQuery(SelectQuerySpecificationNode node)
    {
        if (node.HasAttribute(AstAttributeKeys.ContextKey))
        {
            return node.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        }
        var context = CreateSourceContext(node);
        context.ParentContexts = _parents;
        node.SetAttribute(AstAttributeKeys.ContextKey, context);
        return context;
    }

    private SelectCommandContext CreateSourceContext(SelectQuerySpecificationNode querySpecificationNode)
    {
        void DisposeRowsInputs(List<IRowsInput> rowsInputs)
        {
            foreach (var rowsInput in rowsInputs)
            {
                (rowsInput as IDisposable)?.Dispose();
            }
        }

        IRowsInput CreateInputSourceFromSubQuery(SelectQueryExpressionBodyNode queryExpressionBodyNode)
        {
            CreateForQuery(queryExpressionBodyNode.Queries);
            new SelectBodyNodeVisitor(_executionThread).Run(queryExpressionBodyNode);
            if (queryExpressionBodyNode.GetFunc().Invoke().AsObject is not IRowsIterator iterator)
            {
                throw new QueryCatException("No iterator for subquery!");
            }
            return new RowsIteratorInput(iterator);
        }

        IRowsInput CreateInputSourceFromTableFunction(SelectTableFunctionNode tableFunctionNode)
        {
            _resolveTypesVisitor.Run(tableFunctionNode);
            var source = new CreateDelegateVisitor(_executionThread)
                .RunAndReturn(tableFunctionNode.TableFunction).Invoke();
            var isSubQuery = _parents.Length > 0;
            return CreateRowsInput(source, isSubQuery);
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
                rowsInput = CreateInputSourceFromTableFunction(tableFunctionNode);
                if (_rowsInputContextMap.TryGetValue(rowsInput, out var queryContext))
                {
                    inputContexts.Add(queryContext);
                }
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
            RowsInputIterator = resultRowsIterator as RowsInputIterator,
            InputQueryContextList = inputContexts.ToArray(),
        };
    }

    private IRowsInput CreateRowsInput(VariantValue source, bool isSubQuery)
    {
        if (DataTypeUtils.IsSimple(source.GetInternalType()))
        {
            return new SingleValueRowsInput(source);
        }
        if (source.AsObject is IRowsInput rowsInput)
        {
            var queryContext = new SelectInputQueryContext(rowsInput)
            {
                InputConfigStorage = _executionThread.InputConfigStorage
            };
            if (isSubQuery)
            {
                rowsInput = new CacheRowsInput(rowsInput);
            }
            _rowsInputContextMap[rowsInput] = queryContext;
            rowsInput.SetContext(queryContext);
            rowsInput.Open();
            Logger.Instance.Debug($"Open rows input {rowsInput}.", nameof(SelectSpecificationNodeVisitor));
            return rowsInput;
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            return new RowsIteratorInput(rowsIterator);
        }

        throw new QueryCatException("Invalid rows input.");
    }

    private IRowsIterator CreateMultipleIterator(List<IRowsInput> rowsInputs)
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

        var iterator = node.GetAttribute<IRowsIterator>(AstAttributeKeys.ResultKey);
        if (iterator != null)
        {
            foreach (var column in iterator.Columns)
            {
                column.SourceName = alias;
            }
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
}
