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
    private readonly SelectCommandContext? _parentContext;
    private readonly List<SelectQueryNode> _processed = new();

    public CreateContextVisitor(
        ExecutionThread executionThread,
        SelectCommandContext? parentContext = null)
    {
        _executionThread = executionThread;
        _resolveTypesVisitor = new ResolveTypesVisitor(executionThread);
        _parentContext = parentContext;
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        AstTraversal.PreOrder(node);

        foreach (var processedNode in _processed.AsEnumerable().Reverse())
        {
            new SpecificationNodeVisitor(_executionThread).Run(processedNode);
        }
    }

    /// <inheritdoc />
    public override void Visit(SelectQueryCombineNode node)
    {
        _processed.Add(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        var context = node.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        FillInputCteList(context, node);
        FillContextInitialInput(node, context, context.Parent ?? _parentContext);

        _processed.Add(node);
    }

    private void FillInputCteList(SelectCommandContext context, SelectQuerySpecificationNode node)
    {
        context.CteList.AddRange(GetParentCteList(context));
        if (node.WithNode == null)
        {
            return;
        }

        foreach (var withNodeItem in node.WithNode.Nodes)
        {
            var cteCreateContextVisitor = new CreateContextVisitor(_executionThread, context);
            cteCreateContextVisitor.Run(withNodeItem.QueryNode);
            var cte = new CommonTableExpression(
                withNodeItem.Name,
                withNodeItem.QueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey));
            context.CteList.Add(cte);
            new SpecificationNodeVisitor(_executionThread).Run(withNodeItem);
        }
    }

    private static IEnumerable<CommonTableExpression> GetParentCteList(SelectCommandContext context)
    {
        var parentContext = context.Parent;
        while (parentContext != null)
        {
            foreach (var cte in parentContext.CteList)
            {
                yield return cte;
            }
            parentContext = parentContext.Parent;
        }
    }

    private void FillContextInitialInput(
        SelectQueryNode queryNode,
        SelectCommandContext? parent = null)
    {
        var context = queryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        if (context.RowsInputIterator != null)
        {
            // Seems it has already been filled - skip.
            return;
        }

        if (queryNode is SelectQuerySpecificationNode querySpecificationNode)
        {
            FillContextInitialInput(querySpecificationNode, context, parent);
            return;
        }
        if (queryNode is SelectQueryCombineNode queryCombineNode)
        {
            FillContextInitialInput(queryCombineNode, context, parent);
            return;
        }
        throw new InvalidOperationException($"{queryNode.GetType().Name} cannot be processed.");
    }

    private void FillContextInitialInput(
        SelectQuerySpecificationNode querySpecificationNode,
        SelectCommandContext context,
        SelectCommandContext? parent = null)
    {
        // No FROM - assumed this is the query with SELECT only.
        if (querySpecificationNode.TableExpressionNode == null)
        {
            context.SetIterator(new SingleValueRowsInput().AsIterable());
            return;
        }

        // Start with FROM statement, if none - there is only one SELECT row.
        var finalRowsInputs = new List<IRowsInput>();
        var inputContexts = new List<SelectInputQueryContext>();
        foreach (var tableExpression in querySpecificationNode.TableExpressionNode.Tables.TableFunctions)
        {
            var rowsInputs = GetRowsInputFromExpression(context, tableExpression, querySpecificationNode);
            var finalRowInput = rowsInputs.Last();
            var alias = tableExpression is ISelectAliasNode selectAlias ? selectAlias.Alias : string.Empty;

            SetAlias(tableExpression, alias);
            finalRowsInputs.Add(finalRowInput);
        }

        var resultRowsIterator = CreateMultipleIterator(finalRowsInputs);
        context.RowsInputIterator = resultRowsIterator as RowsInputIterator;
        context.InputQueryContextList = inputContexts.ToArray();
        context.SetIterator(resultRowsIterator);
    }

    private void FillContextInitialInput(
        SelectQueryCombineNode queryCombineNode,
        SelectCommandContext context,
        SelectCommandContext? parent = null)
    {
        FillContextInitialInput(queryCombineNode.LeftQueryNode, parent);
        var leftContext = queryCombineNode.LeftQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        FillContextInitialInput(queryCombineNode.RightQueryNode, parent);
        var rightContext = queryCombineNode.RightQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var combineRowsIterator = new CombineRowsIterator(
            leftContext.CurrentIterator,
            rightContext.CurrentIterator,
            ConvertCombineType(queryCombineNode.CombineType),
            queryCombineNode.IsDistinct);
        context.SetIterator(combineRowsIterator);
    }

    private static CombineType ConvertCombineType(SelectQueryCombineType combineType) => combineType switch
    {
        SelectQueryCombineType.Except => CombineType.Except,
        SelectQueryCombineType.Intersect => CombineType.Intersect,
        SelectQueryCombineType.Union => CombineType.Union,
        _ => throw new NotImplementedException($"{combineType} is not implemented."),
    };

    private IRowsInput[] CreateInputSourceFromCte(SelectCommandContext currentContext, IdentifierExpressionNode idNode)
    {
        var cteIndex = currentContext.CteList.FindIndex(c => c.Name == idNode.FullName);
        if (cteIndex < 0)
        {
            throw new InvalidOperationException($"Query with name '{idNode.FullName}' is not defined.");
        }
        var context = currentContext.CteList[cteIndex].Context;
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
        SelectCommandContext context,
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
            rowsInput = CreateInputSourceFromTableJoin(context, rowsInput, joinedNode);
            inputs.Add(rowsInput);
        }
        return inputs.ToArray();
    }

    private IRowsInput CreateInputSourceFromTableJoin(SelectCommandContext context, IRowsInput left,
        SelectTableJoinedNode tableJoinedNode)
    {
        var right = GetRowsInputFromExpression(context, tableJoinedNode.RightTableNode).Last();
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
        SelectCommandContext context,
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
            return CreateInputSourceFromTableFunction(context, tableFunctionNode);
        }
        else if (expressionNode is IdentifierExpressionNode idNode)
        {
            return CreateInputSourceFromCte(context, idNode);
        }
        else
        {
            throw new InvalidOperationException($"Cannot process node '{expressionNode}' as input.");
        }
    }

    private IRowsInput CreateInputSourceFromSubQuery(
        SelectQueryNode queryBodyNode,
        SelectQuerySpecificationNode? parentSpecificationNode = null)
    {
        FillContextInitialInput(queryBodyNode);
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
