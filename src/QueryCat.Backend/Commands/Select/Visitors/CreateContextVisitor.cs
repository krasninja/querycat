using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed class CreateContextVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;
    private readonly ResolveTypesVisitor _resolveTypesVisitor;
    private readonly Dictionary<IRowsInput, SelectInputQueryContext> _rowsInputContextMap = new();
    private readonly SelectCommandContext? _parentContext;

    private record struct Cte(string Name, SelectCommandContext Context);

    private Cte[] _ctes = Array.Empty<Cte>();

    public CreateContextVisitor(
        ExecutionThread executionThread,
        SelectCommandContext? parentContext = null)
    {
        this._executionThread = executionThread;
        this._resolveTypesVisitor = new ResolveTypesVisitor(executionThread);
        this._parentContext = parentContext;
    }

    private CreateContextVisitor(
        ExecutionThread executionThread,
        SelectCommandContext? parentContext = null,
        Cte[]? ctes = null)
        : this(executionThread, parentContext)
    {
        this._ctes = ctes ?? Array.Empty<Cte>();
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

        _ctes = CreateInputCtes(node).Union(_ctes).ToArray();

        var parentTableExpressionNode = AstTraversal.GetFirstParent<SelectQuerySpecificationNode>(n => n.Id != node.Id);
        var parentContext = parentTableExpressionNode?.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var context = CreateSourceContext(node, parentContext ?? _parentContext);
        node.SetAttribute(AstAttributeKeys.ContextKey, context);
    }

    private List<Cte> CreateInputCtes(SelectQuerySpecificationNode node)
    {
        if (node.WithNode == null)
        {
            return new List<Cte>();
        }

        var ctes = new List<Cte>();
        foreach (var withNodeItem in node.WithNode.Nodes)
        {
            var cteCreateContextVisitor = new CreateContextVisitor(_executionThread, _parentContext, ctes.ToArray());
            cteCreateContextVisitor.Run(withNodeItem.QueryNode);
            new SpecificationNodeVisitor(_executionThread).Run(withNodeItem);
            var cte = new Cte(withNodeItem.Name, withNodeItem.QueryNode
                .GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey));
            ctes.Add(cte);
        }
        return ctes;
    }

    private SelectCommandContext CreateForQuery(SelectQueryNode node, SelectCommandContext? parent = null)
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
        SelectQueryNode queryNode,
        SelectCommandContext? parent = null)
    {
        if (queryNode is SelectQuerySpecificationNode querySpecificationNode)
        {
            return CreateSourceContext(querySpecificationNode, parent);
        }
        if (queryNode is SelectQueryCombineNode queryCombineNode)
        {
            return CreateSourceContext(queryCombineNode, parent);
        }
        throw new InvalidOperationException($"{queryNode.GetType().Name} cannot be processed.");
    }

    private SelectCommandContext CreateSourceContext(
        SelectQuerySpecificationNode querySpecificationNode,
        SelectCommandContext? parent = null)
    {
        // No FROM - assumed this is the query with SELECT only.
        if (querySpecificationNode.TableExpressionNode == null)
        {
            return new(new SingleValueRowsInput().AsIterable());
        }

        // Start with FROM statement, if none - there is only one SELECT row.
        var finalRowsInputs = new List<IRowsInput>();
        var inputContexts = new List<SelectInputQueryContext>();
        foreach (var tableExpression in querySpecificationNode.TableExpressionNode.Tables.TableFunctions)
        {
            var rowsInputs = GetRowsInputFromExpression(tableExpression, querySpecificationNode);
            var finalRowInput = rowsInputs.Last();
            var alias = tableExpression is ISelectAliasNode selectAlias ? selectAlias.Alias : string.Empty;
            foreach (var rowsInput in rowsInputs)
            {
                if (_rowsInputContextMap.TryGetValue(rowsInput, out var queryContext))
                {
                    inputContexts.Add(queryContext);
                }
            }

            SetAlias(tableExpression, alias);
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

    private SelectCommandContext CreateSourceContext(
        SelectQueryCombineNode queryCombineNode,
        SelectCommandContext? parent = null)
    {
        var leftContext = CreateSourceContext(queryCombineNode.LeftQueryNode, parent);
        var rightContext = CreateSourceContext(queryCombineNode.RightQueryNode, parent);
        var combineRowsIterator = new CombineRowsIterator(
            leftContext.CurrentIterator,
            rightContext.CurrentIterator,
            ConvertCombineType(queryCombineNode.CombineType),
            queryCombineNode.IsDistinct);
        var context = new SelectCommandContext(combineRowsIterator);
        queryCombineNode.SetAttribute(AstAttributeKeys.ContextKey, queryCombineNode);
        return context;
    }

    private static CombineType ConvertCombineType(SelectQueryCombineType combineType) => combineType switch
    {
        SelectQueryCombineType.Except => CombineType.Except,
        SelectQueryCombineType.Intersect => CombineType.Intersect,
        SelectQueryCombineType.Union => CombineType.Union,
        _ => throw new NotImplementedException($"{combineType} is not implemented."),
    };

    private IRowsInput[] CreateInputSourceFromCte(IdentifierExpressionNode idNode)
    {
        var cteIndex = Array.FindIndex(_ctes, c => c.Name == idNode.FullName);
        if (cteIndex < 0)
        {
            throw new InvalidOperationException($"Query with name '{idNode.FullName}' is not defined.");
        }
        var context = _ctes[cteIndex].Context;
        if (context.RowsInputIterator == null)
        {
            throw new InvalidOperationException("Invalid CTE.");
        }
        return new[]
        {
            new RowsIteratorInput(context.CurrentIterator),
        };
    }

    // Last input is combine input.
    private IRowsInput[] CreateInputSourceFromTableFunction(
        SelectTableFunctionNode tableFunctionNode,
        SelectCommandContext? parent = null)
    {
        _resolveTypesVisitor.Run(tableFunctionNode.TableFunction);
        var inputs = new List<IRowsInput>();
        var rowsInput = tableFunctionNode.GetRequiredAttribute<IRowsInput>(AstAttributeKeys.RowsInputKey);
        inputs.Add(rowsInput);
        SetAlias(rowsInput, tableFunctionNode.Alias);
        foreach (var joinedNode in tableFunctionNode.JoinedNodes)
        {
            rowsInput = CreateInputSourceFromTableJoin(rowsInput, joinedNode);
            inputs.Add(rowsInput);
        }
        return inputs.ToArray();
    }

    private IRowsInput CreateInputSourceFromTableJoin(IRowsInput left, SelectTableJoinedNode tableJoinedNode)
    {
        var right = GetRowsInputFromExpression(tableJoinedNode.RightTableNode).Last();
        var alias = tableJoinedNode.RightTableNode is ISelectAliasNode selectAlias ? selectAlias.Alias : string.Empty;
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
        right = new CacheRowsInput(right);

        new InputResolveTypesVisitor(_executionThread, left, right)
            .Run(tableJoinedNode.SearchConditionNode);
        var searchFunc = new InputCreateDelegateVisitor(_executionThread, left, right)
            .RunAndReturn(tableJoinedNode.SearchConditionNode);
        return new SelectJoinRowsInput(left, right, join, searchFunc, reverseColumnsOrder);
    }

    private IRowsInput[] GetRowsInputFromExpression(
        ExpressionNode expressionNode,
        SelectQuerySpecificationNode? parentSpecificationNode = null)
    {
        if (expressionNode is SelectQueryNode queryNode)
        {
            return new[]
            {
                CreateInputSourceFromSubQuery(queryNode, parentSpecificationNode)
            };
        }
        else if (expressionNode is SelectTableFunctionNode tableFunctionNode)
        {
            return CreateInputSourceFromTableFunction(tableFunctionNode);
        }
        else if (expressionNode is IdentifierExpressionNode idNode)
        {
            return CreateInputSourceFromCte(idNode);
        }
        else
        {
            throw new InvalidOperationException($"Cannot process node {expressionNode} as input.");
        }
    }

    private IRowsInput CreateInputSourceFromSubQuery(
        SelectQueryNode queryBodyNode,
        SelectQuerySpecificationNode? parentSpecificationNode = null)
    {
        CreateForQuery(queryBodyNode);
        new CreateContextVisitor(_executionThread, null).Run(queryBodyNode);
        new SpecificationNodeVisitor(_executionThread, parentSpecificationNode).Run(queryBodyNode);
        var commandContext = queryBodyNode.GetRequiredAttribute<CommandContext>(AstAttributeKeys.ContextKey);
        if (commandContext.Invoke().AsObject is not IRowsIterator iterator)
        {
            throw new QueryCatException("No iterator for subquery!");
        }
        var rowsInput = new RowsIteratorInput(iterator);
        SetAlias(rowsInput, queryBodyNode.Alias);
        return rowsInput;
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
}
