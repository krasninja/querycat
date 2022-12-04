using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed class CreateContextVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;
    private readonly ResolveTypesVisitor _resolveTypesVisitor;
    private readonly Dictionary<IRowsInput, SelectInputQueryContext> _rowsInputContextMap = new();
    private readonly SelectCommandContext? _parentContext;

    /// <summary>
    /// AST traversal.
    /// </summary>
    public AstTraversal AstTraversal { get; }

    public CreateContextVisitor(ExecutionThread executionThread, SelectCommandContext? parentContext = null)
    {
        this._executionThread = executionThread;
        this._resolveTypesVisitor = new ResolveTypesVisitor(executionThread);
        this._parentContext = parentContext;
        this.AstTraversal = new AstTraversal(this);
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        AstTraversal.PreOrder(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        if (node.HasAttribute(AstAttributeKeys.ContextKey))
        {
            return;
        }

        var parentTableExpressionNode = AstTraversal.GetFirstParent<SelectQuerySpecificationNode>(n => n.Id != node.Id);
        var parentContext = parentTableExpressionNode?.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        var context = CreateSourceContext(node, parentContext ?? _parentContext);
        node.SetAttribute(AstAttributeKeys.ContextKey, context);

        base.Visit(node);
    }

    public IList<SelectCommandContext> CreateForQuery(IEnumerable<SelectQuerySpecificationNode> nodes,
        SelectCommandContext? parent = null)
    {
        return nodes.Select(n => CreateForQuery(n, parent)).ToList();
    }

    public SelectCommandContext CreateForQuery(SelectQuerySpecificationNode node, SelectCommandContext? parent = null)
    {
        if (node.HasAttribute(AstAttributeKeys.ContextKey))
        {
            return node.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        }
        var context = CreateSourceContext(node);
        if (parent != null)
        {
            context.SetParent(parent);
        }
        node.SetAttribute(AstAttributeKeys.ContextKey, context);
        return context;
    }

    private SelectCommandContext CreateSourceContext(
        SelectQuerySpecificationNode querySpecificationNode,
        SelectCommandContext? parent = null)
    {
        IRowsInput CreateInputSourceFromSubQuery(SelectQueryExpressionBodyNode queryExpressionBodyNode)
        {
            CreateForQuery(queryExpressionBodyNode.Queries, parent);
            new CreateContextVisitor(_executionThread, parent).Run(queryExpressionBodyNode);
            new SpecificationNodeVisitor(_executionThread).Run(queryExpressionBodyNode);
            var commandContext = queryExpressionBodyNode.GetRequiredAttribute<CommandContext>(AstAttributeKeys.ContextKey);
            if (commandContext.Invoke().AsObject is not IRowsIterator iterator)
            {
                throw new QueryCatException("No iterator for subquery!");
            }
            var rowsInput = new RowsIteratorInput(iterator);
            SetAlias(rowsInput, queryExpressionBodyNode.Alias);
            return rowsInput;
        }

        // Last input is combine input.
        IRowsInput[] CreateInputSourceFromTableFunction(SelectTableFunctionNode tableFunctionNode)
        {
            _resolveTypesVisitor.Run(tableFunctionNode.TableFunction);
            var source = new CreateDelegateVisitor(_executionThread)
                .RunAndReturn(tableFunctionNode.TableFunction).Invoke();
            var isSubQuery = parent != null;
            var inputs = new List<IRowsInput>();
            var rowsInput = CreateRowsInput(source, isSubQuery);
            var parentSpecificationNodes = AstTraversal.GetParents<SelectQuerySpecificationNode>().ToList();
            FixInputColumnTypes(parentSpecificationNodes, rowsInput);
            inputs.Add(rowsInput);
            SetAlias(rowsInput, tableFunctionNode.Alias);
            foreach (var joinedNode in tableFunctionNode.JoinedNodes)
            {
                rowsInput = CreateInputSourceFromTableJoin(rowsInput, joinedNode);
                inputs.Add(rowsInput);
            }
            return inputs.ToArray();
        }

        IRowsInput CreateInputSourceFromTableJoin(IRowsInput left, SelectTableJoinedNode tableJoinedNode)
        {
            var right = GetRowsInputFromExpression(tableJoinedNode.RightTableNode).Last();
            var alias = GetAliasFromExpression(tableJoinedNode.RightTableNode);
            SetAlias(right, alias);

            // For right join we swap left and right. But we keep columns in the same order.
            var join = ConvertAstJoinType(tableJoinedNode.JoinTypeNode.JoinedType);
            var reverseColumnsOrder = false;
            if (join == JoinType.Right)
            {
                (left, right) = (right, left);
                reverseColumnsOrder = true;
            }
            // Because of iterator specific conditions we must cache right input.
            if (_rowsInputContextMap.TryGetValue(right, out var context))
            {
                right = new CacheRowsInput(right);
                right.SetContext(context);
            }

            new InputResolveTypesVisitor(_executionThread, left, right)
                .Run(tableJoinedNode.SearchConditionNode);
            var searchFunc = new InputCreateDelegateVisitor(_executionThread, left, right)
                .RunAndReturn(tableJoinedNode.SearchConditionNode);
            return new SelectJoinRowsInput(left, right, join, searchFunc, reverseColumnsOrder);
        }

        IRowsInput[] GetRowsInputFromExpression(ExpressionNode expressionNode)
        {
            if (expressionNode is SelectQueryExpressionBodyNode selectQueryExpressionBodyNode)
            {
                return new[]
                {
                    CreateInputSourceFromSubQuery(selectQueryExpressionBodyNode)
                };
            }
            else if (expressionNode is SelectTableFunctionNode tableFunctionNode)
            {
                return CreateInputSourceFromTableFunction(tableFunctionNode);
            }
            else
            {
                throw new InvalidOperationException($"Cannot process node {expressionNode} as input.");
            }
        }

        string GetAliasFromExpression(ExpressionNode expressionNode)
        {
            if (expressionNode is SelectQueryExpressionBodyNode selectQueryExpressionBodyNode)
            {
                return selectQueryExpressionBodyNode.Alias;
            }
            else if (expressionNode is SelectTableFunctionNode tableFunctionNode)
            {
                return tableFunctionNode.Alias;
            }
            return string.Empty;
        }

        //
        // Entry point here.
        //

        // No FROM - assumed this is the query with SELECT only.
        if (querySpecificationNode.TableExpression == null)
        {
            return new(new SingleValueRowsInput().AsIterable());
        }

        // Start with FROM statement, if none - there is only one SELECT row.
        var finalRowsInputs = new List<IRowsInput>();
        var inputContexts = new List<SelectInputQueryContext>();
        foreach (var tableExpression in querySpecificationNode.TableExpression.Tables.TableFunctions)
        {
            var rowsInputs = GetRowsInputFromExpression(tableExpression);
            var finalRowInput = rowsInputs.Last();
            var alias = GetAliasFromExpression(tableExpression);
            foreach (var rowsInput in rowsInputs)
            {
                if (_rowsInputContextMap.TryGetValue(rowsInput, out var queryContext))
                {
                    inputContexts.Add(queryContext);
                }
            }

            SetAlias(tableExpression, alias);
            tableExpression.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInputs);
            finalRowsInputs.Add(finalRowInput);
        }

        var resultRowsIterator = CreateMultipleIterator(finalRowsInputs);

        var context = new SelectCommandContext(resultRowsIterator)
        {
            RowsInputIterator = resultRowsIterator as RowsInputIterator,
            InputQueryContextList = inputContexts.ToArray(),
        };
        if (parent != null)
        {
            context.SetParent(parent);
        }
        return context;
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
            Logger.Instance.Debug($"Open rows input {rowsInput}.", nameof(CreateContextVisitor));
            return rowsInput;
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            return new RowsIteratorInput(rowsIterator);
        }

        throw new QueryCatException("Invalid rows input.");
    }

    private static IRowsIterator CreateMultipleIterator(List<IRowsInput> rowsInputs)
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

    private static void SetAlias(IAstNode node, string alias)
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

    private static void SetAlias(IRowsInput input, string alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return;
        }
        foreach (var column in input.Columns)
        {
            column.SourceName = alias;
        }
    }

    private static JoinType ConvertAstJoinType(SelectTableJoinedType tableJoinedType)
        => tableJoinedType switch
        {
            SelectTableJoinedType.Full => JoinType.Full,
            SelectTableJoinedType.Inner => JoinType.Inner,
            SelectTableJoinedType.Left => JoinType.Left,
            SelectTableJoinedType.Right => JoinType.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(tableJoinedType), tableJoinedType, null)
        };

    /// <summary>
    /// Find the expressions in SELECT output area like CAST(id AS string).
    /// </summary>
    private static void FixInputColumnTypes(
        IEnumerable<SelectQuerySpecificationNode> querySpecificationNodes,
        IRowsInput rowsInput)
    {
        foreach (var querySpecificationNode in querySpecificationNodes)
        {
            foreach (var castNode in querySpecificationNode.ColumnsList.GetAllChildren<CastFunctionNode>())
            {
                if (castNode.ExpressionNode is not IdentifierExpressionNode idNode)
                {
                    continue;
                }

                var columnIndex = rowsInput.GetColumnIndexByName(idNode.Name, idNode.SourceName);
                if (columnIndex > -1)
                {
                    rowsInput.Columns[columnIndex].DataType = castNode.TargetTypeNode.Type;
                }
            }
        }
    }
}
