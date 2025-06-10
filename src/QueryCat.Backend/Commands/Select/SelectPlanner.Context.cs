using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    public async Task<SelectCommandContext> Context_CreateAsync(
        SelectQueryNode node,
        SelectCommandContext? parentContext = null,
        CancellationToken cancellationToken = default)
    {
        if (node is SelectQuerySpecificationNode querySpecificationNode)
        {
            return await Context_CreateAsync(querySpecificationNode, parentContext, cancellationToken);
        }
        if (node is SelectQueryCombineNode queryCombineNode)
        {
            return await Context_CreateAsync(queryCombineNode, parentContext, cancellationToken);
        }
        throw new InvalidOperationException(string.Format(Resources.Errors.NotSupported, node.GetType().Name));
    }

    public async Task<SelectCommandContext> Context_CreateAsync(
        SelectQuerySpecificationNode node,
        SelectCommandContext? parentContext = null,
        CancellationToken cancellationToken = default)
    {
        await Misc_TransformAsync(node, cancellationToken);
        var context = Context_CreateInitialContext(node, parentContext);
        await Context_InitializeRowsInputsAsync(context, node, cancellationToken);
        await ContextCte_PrepareInputListAsync(context, node, cancellationToken);
        await Context_PrepareInitialInputsAsync(context, node, cancellationToken);
        return context;
    }

    public async Task<SelectCommandContext> Context_CreateAsync(
        SelectQueryCombineNode node,
        SelectCommandContext? parentContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = Context_CreateInitialContext(node, parentContext);
        await Context_InitializeRowsInputsAsync(context, node, cancellationToken);
        return context;
    }

    private SelectCommandContext Context_CreateInitialContext(SelectQueryNode node, SelectCommandContext? parentContext = null)
    {
        var context = new SelectCommandContext(node);
        context.CapturedScope = ExecutionThread.TopScope;
        context.SetParent(parentContext);
        node.SetAttribute(AstAttributeKeys.ContextKey, context);
        return context;
    }

    private async Task Context_InitializeRowsInputsAsync(SelectCommandContext context, AstNode node, CancellationToken cancellationToken)
    {
        await new CreateRowsInputVisitor(ExecutionThread, context).RunAsync(node, cancellationToken);
    }

    private async Task Context_PrepareInitialInputAsync(SelectQueryNode queryNode, CancellationToken cancellationToken)
    {
        var context = queryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        if (context.FirstRowsInput != null)
        {
            // Seems it has already been filled - skip.
            return;
        }

        if (queryNode is SelectQuerySpecificationNode querySpecificationNode)
        {
            await Context_PrepareInitialInputsAsync(context, querySpecificationNode, cancellationToken);
            return;
        }
        if (queryNode is SelectQueryCombineNode queryCombineNode)
        {
            await Context_PrepareInitialInputAsync(context, queryCombineNode, cancellationToken);
            return;
        }
        throw new InvalidOperationException($"{queryNode.GetType().Name} cannot be processed.");
    }

    private async Task Context_PrepareInitialInputsAsync(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode,
        CancellationToken cancellationToken)
    {
        // Three is no FROM node - assume this is the query with SELECT only.
        if (querySpecificationNode.TableExpressionNode == null)
        {
            context.SetIterator(new SingleValueRowsInput().AsIterable());
            return;
        }

        // Start with FROM statement, if none - there is only one SELECT row.
        var finalRowsInputs = new List<IRowsInput>();
        foreach (var tableExpression in querySpecificationNode.TableExpressionNode.TablesNode.TableFunctionsNodes)
        {
            var rowsInputs = await Context_GetRowsInputFromExpressionAsync(context, tableExpression, cancellationToken);
            if (rowsInputs.Length == 0)
            {
                throw new QueryCatException(string.Format(Resources.Errors.CannotResolveInputSource, tableExpression));
            }
            var finalRowInput = rowsInputs[^1];
            var alias = tableExpression is ISelectAliasNode selectAlias ? selectAlias.Alias : string.Empty;

            Context_SetAlias(tableExpression, alias);
            finalRowInput = Context_WrapKeysInput(finalRowInput, context);
            finalRowsInputs.Add(finalRowInput);
        }

        // Fix types.
        foreach (var input in finalRowsInputs.OfType<IRowsSchema>())
        {
            FixInputColumnTypes(querySpecificationNode, input);
        }

        // Create final iterator.
        var resultRowsIterator = Context_CreateMultipleIterator(finalRowsInputs,
            ExecutionThread.Statistic, ExecutionThread.Options.ShowDetailedStatistic);
        await QueryContext_FillQueryContextConditionsAsync(context, querySpecificationNode, cancellationToken);
        context.SetIterator(resultRowsIterator);
    }

    private async Task Context_PrepareInitialInputAsync(
        SelectCommandContext context,
        SelectQueryCombineNode queryCombineNode,
        CancellationToken cancellationToken)
    {
        await Context_PrepareInitialInputAsync(queryCombineNode.LeftQueryNode, cancellationToken);
        var leftContext = queryCombineNode.LeftQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        await Context_PrepareInitialInputAsync(queryCombineNode.RightQueryNode, cancellationToken);
        var rightContext = queryCombineNode.RightQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var combineRowsIterator = new CombineRowsIterator(
            leftContext.CurrentIterator,
            rightContext.CurrentIterator,
            Context_ConvertCombineType(queryCombineNode.CombineType),
            queryCombineNode.IsDistinct);
        context.SetIterator(combineRowsIterator);
    }

    private static CombineType Context_ConvertCombineType(SelectQueryCombineType combineType) => combineType switch
    {
        SelectQueryCombineType.Except => CombineType.Except,
        SelectQueryCombineType.Intersect => CombineType.Intersect,
        SelectQueryCombineType.Union => CombineType.Union,
        _ => throw new ArgumentOutOfRangeException(nameof(combineType), string.Format(Resources.Errors.NotImplemented, combineType)),
    };

    private async Task<IRowsInput[]> Context_CreateInputSourceFromCteAsync(
        SelectCommandContext context,
        IdentifierExpressionNode idNode,
        CancellationToken cancellationToken)
    {
        var cteIndex = context.CteList.FindIndex(c => c.Name == idNode.FullName);
        if (cteIndex < 0)
        {
            return [];
        }
        var inputs = new List<IRowsInput>
        {
            context.CteList[cteIndex].RowsInputProxy,
        };
        if (idNode is ISelectAliasNode aliasNode)
        {
            Context_SetAlias(inputs[0], aliasNode.Alias);
        }
        if (idNode is SelectIdentifierExpressionNode selectIdentifierExpressionNode)
        {
            foreach (var joinedNode in selectIdentifierExpressionNode.JoinedNodes)
            {
                var joinRowsInput = await Context_CreateInputSourceFromTableJoinAsync(
                    context, inputs[0], joinedNode, cancellationToken);
                inputs.Add(joinRowsInput);
            }
        }
        return inputs.ToArray();
    }

    private async Task<IRowsInput[]> Context_CreateInputSourceFromVariableAsync(
        SelectCommandContext context,
        IdentifierExpressionNode idNode,
        CancellationToken cancellationToken)
    {
        var rowsInput = idNode.GetAttribute<IRowsInput>(AstAttributeKeys.RowsInputKey);
        if (rowsInput == null)
        {
            return [];
        }

        // Alias.
        if (idNode is ISelectAliasNode selectAliasNode && !string.IsNullOrEmpty(selectAliasNode.Alias))
        {
            Context_SetAlias(rowsInput, selectAliasNode.Alias);
        }

        var result = new[] { rowsInput };

        // Joined nodes processing.
        if (idNode is SelectIdentifierExpressionNode selectIdentifierExpressionNode)
        {
            var joinedInputs = new List<IRowsInput>(capacity: selectIdentifierExpressionNode.JoinedNodes.Count + 1)
            {
                rowsInput
            };
            foreach (var joinedNode in selectIdentifierExpressionNode.JoinedNodes)
            {
                var joinRowsInput = await Context_CreateInputSourceFromTableJoinAsync(
                    context, rowsInput, joinedNode, cancellationToken);
                joinedInputs.Add(joinRowsInput);
            }
            result = joinedInputs.ToArray();
        }

        return result;
    }

    // Last input is combine input.
    private async Task<IRowsInput[]> Context_CreateInputSourceFromTableFunctionAsync(
        SelectCommandContext context,
        SelectTableFunctionNode tableFunctionNode,
        CancellationToken cancellationToken)
    {
        var inputs = new List<IRowsInput>();
        var rowsInput = tableFunctionNode.GetRequiredAttribute<IRowsInput>(AstAttributeKeys.RowsInputKey);
        inputs.Add(rowsInput);
        Context_SetAlias(rowsInput, tableFunctionNode.Alias);
        foreach (var joinedNode in tableFunctionNode.JoinedNodes)
        {
            rowsInput = await Context_CreateInputSourceFromTableJoinAsync(context, rowsInput, joinedNode, cancellationToken);
            inputs.Add(rowsInput);
        }
        return inputs.ToArray();
    }

    private async Task<IRowsInput> Context_CreateInputSourceFromTableJoinAsync(
        SelectCommandContext context,
        IRowsInput left,
        SelectTableJoinedNode tableJoinedNode,
        CancellationToken cancellationToken)
    {
        var right = (await Context_GetRowsInputFromExpressionAsync(context, tableJoinedNode.RightTableNode, cancellationToken))
            .Last();
        var alias = tableJoinedNode.RightTableNode is ISelectAliasNode selectAlias ? selectAlias.Alias : string.Empty;
        Context_SetAlias(right, alias);
        right = Context_WrapKeysInput(right, context);
        left = Context_WrapKeysInput(left, context);

        // For right join we swap left and right. But we keep columns in the same order.
        var join = Context_ConvertAstJoinType(tableJoinedNode.JoinTypeNode.JoinedType);
        var reverseColumnsOrder = false;
        if (join == JoinType.Right)
        {
            (left, right) = (right, left);
            reverseColumnsOrder = true;
        }
        // Because of iterator specific conditions we better cache right input. Consider that resetting rows input
        // might be resource consuming operation.
        if (!ExecutionThread.Options.DisableCache && Context_CanUseInputCache(right))
        {
            right = new CacheRowsInput(ExecutionThread, right, context.Conditions);
        }

        if (tableJoinedNode is SelectTableJoinedOnNode joinedOnNode)
        {
            var searchFunc = await new InputCreateDelegateVisitor(ExecutionThread, context, left, right)
                .RunAndReturnAsync(joinedOnNode.SearchConditionNode, cancellationToken);
            return new SelectJoinRowsInput(ExecutionThread, left, right, join, searchFunc, reverseColumnsOrder);
        }
        if (tableJoinedNode is SelectTableJoinedUsingNode joinedUsingNode)
        {
            var searchFunc = await new InputCreateDelegateVisitor(ExecutionThread, context, left, right)
                .RunAndReturnAsync(joinedUsingNode, cancellationToken);
            return new SelectJoinRowsInput(ExecutionThread, left, right, join, searchFunc, reverseColumnsOrder);
        }
        throw new ArgumentException(string.Format(Resources.Errors.NotSupported, tableJoinedNode.GetType().Name),
            nameof(tableJoinedNode));
    }

    private IRowsInput Context_WrapKeysInput(IRowsInput rowsInput, SelectCommandContext context)
    {
        if (rowsInput is IRowsInputKeys rowsInputKeys
            && rowsInputKeys is not SetKeysRowsInput
            && context.Inputs.Any(i => i.RowsInput == rowsInput) // Create wrapper only for input source with data.
        )
        {
            return new SetKeysRowsInput(ExecutionThread, rowsInputKeys, context.Conditions);
        }
        return rowsInput;
    }

    private async Task<IRowsInput[]> Context_GetRowsInputFromExpressionAsync(
        SelectCommandContext context,
        ExpressionNode expressionNode,
        CancellationToken cancellationToken)
    {
        if (expressionNode is SelectQueryNode queryNode)
        {
            return
            [
                await Context_CreateInputSourceFromSubQueryAsync(context, queryNode, cancellationToken)
            ];
        }
        if (expressionNode is SelectTableFunctionNode tableFunctionNode)
        {
            return await Context_CreateInputSourceFromTableFunctionAsync(context, tableFunctionNode, cancellationToken);
        }
        if (expressionNode is IdentifierExpressionNode idNode)
        {
            var inputs = await Context_CreateInputSourceFromCteAsync(context, idNode, cancellationToken);
            if (inputs.Length == 0)
            {
                inputs = await Context_CreateInputSourceFromVariableAsync(context, idNode, cancellationToken);
            }
            return inputs;
        }
        if (expressionNode is SelectTableValuesNode
            || expressionNode is FunctionCallNode)
        {
            return
            [
                expressionNode.GetRequiredAttribute<IRowsInput>(AstAttributeKeys.RowsInputKey)
            ];
        }

        throw new InvalidOperationException(string.Format(Resources.Errors.CannotProcessNodeAsInput, expressionNode));
    }

    private async Task<IRowsInput> Context_CreateInputSourceFromSubQueryAsync(SelectCommandContext context, SelectQueryNode queryNode,
        CancellationToken cancellationToken)
    {
        var rowsIterator = await CreateIteratorAsync(queryNode, parentContext: context, cancellationToken);

        var rowsInput = new RowsIteratorInput(rowsIterator);
        context.AddInput(new SelectCommandInputContext(rowsInput));
        Context_SetAlias(rowsInput, queryNode.Alias);
        return rowsInput;
    }

    private static IRowsIterator Context_CreateMultipleIterator(
        List<IRowsInput> rowsInputs,
        ExecutionStatistic executionStatistic,
        bool detailedStatistic)
    {
        if (rowsInputs.Count == 0)
        {
            throw new QueryCatException(Resources.Errors.NoInputs);
        }
        if (rowsInputs.Count == 1)
        {
            return new RowsInputIterator(
                rowsInputs[0],
                autoFetch: false,
                statistic: executionStatistic,
                detailedStatistic: detailedStatistic);
        }
        var multipleIterator = new MultiplyRowsIterator(
            new RowsInputIterator(
                rowsInputs[0],
                autoFetch: true,
                statistic: executionStatistic,
                detailedStatistic: detailedStatistic),
            new RowsInputIterator(
                rowsInputs[1],
                autoFetch: true,
                statistic: executionStatistic,
                detailedStatistic: detailedStatistic));
        for (var i = 2; i < rowsInputs.Count; i++)
        {
            multipleIterator = new MultiplyRowsIterator(
                multipleIterator, new RowsInputIterator(
                    rowsInputs[i],
                    autoFetch: true,
                    statistic: executionStatistic,
                    detailedStatistic: detailedStatistic));
        }
        return multipleIterator;
    }

    private static void Context_SetAlias(IAstNode node, string alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return;
        }

        foreach (var inputColumn in node.GetAllChildren<IdentifierExpressionNode>()
                     .Where(n => string.IsNullOrEmpty(n.TableSourceName)))
        {
            inputColumn.TableSourceName = alias;
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

    private static void Context_SetAlias(IRowsInput input, string alias)
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

    /// <summary>
    /// Find the expressions in SELECT output area like CAST(id AS string) or id::string.
    /// </summary>
    private void FixInputColumnTypes(SelectQuerySpecificationNode querySpecificationNode, IRowsSchema rowsSchema)
    {
        foreach (var castNode in querySpecificationNode.ColumnsListNode.GetAllChildren<CastFunctionNode>())
        {
            if (castNode.ExpressionNode is not IdentifierExpressionNode idNode)
            {
                continue;
            }

            var columnIndex = rowsSchema.GetColumnIndexByName(idNode.TableFieldName, idNode.TableSourceName);
            if (columnIndex > -1)
            {
                rowsSchema.Columns[columnIndex].DataType = castNode.TargetTypeNode.Type;
            }
        }
    }

    /// <summary>
    /// The function determines if cache can be applied for the certain input.
    /// </summary>
    /// <param name="input">Rows input.</param>
    /// <returns><c>True</c> if cache can be applied, <c>false</c> otherwise.</returns>
    private static bool Context_CanUseInputCache(IRowsInput input)
    {
        if (input is not IRowsIteratorParent rowsIteratorRoot)
        {
            return true;
        }

        var childNodes = rowsIteratorRoot.GetChildren().ToList();
        while (childNodes.Any())
        {
            // If we have any proxy iterator we cannot guarantee inner input persistence.
            foreach (var child in childNodes)
            {
                if (child is ProxyRowsIterator)
                {
                    return false;
                }
            }
            // If all child nodes are already cache inputs - no need to cache twice.
            if (childNodes.All(n => n is CacheRowsInput))
            {
                return false;
            }

            childNodes = childNodes
                .OfType<IRowsIteratorParent>()
                .SelectMany(n => n.GetChildren())
                .ToList();
        }

        return true;
    }

    private static JoinType Context_ConvertAstJoinType(SelectTableJoinedType tableJoinedType)
        => tableJoinedType switch
        {
            SelectTableJoinedType.Full => JoinType.Full,
            SelectTableJoinedType.Inner => JoinType.Inner,
            SelectTableJoinedType.Left => JoinType.Left,
            SelectTableJoinedType.Right => JoinType.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(tableJoinedType), tableJoinedType, null)
        };
}
