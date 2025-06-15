using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.KeyConditionValue;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    private async Task QueryContext_FillQueryContextConditionsAsync(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode,
        CancellationToken cancellationToken)
    {
        // Fill conditions.
        foreach (var inputContext in context.Inputs)
        {
            await QueryContext_FillQueryContextConditionsAsync(
                querySpecificationNode.TableExpressionNode?.SearchConditionNode?.ExpressionNode,
                inputContext,
                context,
                cancellationToken);
            foreach (var joinedOnNode in querySpecificationNode.GetAllChildren<SelectTableJoinedOnNode>())
            {
                await QueryContext_FillQueryContextConditionsAsync(
                    joinedOnNode.SearchConditionNode,
                    inputContext,
                    context,
                    cancellationToken);
            }
        }

        // Fill "limit". For now, we limit only if order is not defined.
        if (querySpecificationNode.OrderByNode == null)
        {
            if (querySpecificationNode.FetchNode != null)
            {
                var @delegate = await Misc_CreateDelegateAsync(querySpecificationNode.FetchNode.CountNode,
                    cancellationToken: cancellationToken);
                var fetchCount = (await @delegate.InvokeAsync(ExecutionThread, cancellationToken)).AsInteger;
                foreach (var queryContext in context.Inputs)
                {
                    queryContext.QueryInfo.Limit = (queryContext.QueryInfo.Limit ?? 0) + fetchCount;
                }
            }
            if (querySpecificationNode.OffsetNode != null)
            {
                var @delegate = await Misc_CreateDelegateAsync(querySpecificationNode.OffsetNode.CountNode,
                    cancellationToken: cancellationToken);
                var offsetCount = (await @delegate.InvokeAsync(ExecutionThread, cancellationToken)).AsInteger;
                foreach (var queryContext in context.Inputs)
                {
                    queryContext.QueryInfo.Limit = (queryContext.QueryInfo.Limit ?? 0) + offsetCount;
                }
            }
        }
    }

    private async Task QueryContext_FillQueryContextConditionsAsync(
        ExpressionNode? predicateNode,
        SelectInputQueryContext rowsInputContext,
        SelectCommandContext commandContext,
        CancellationToken cancellationToken)
    {
        if (predicateNode == null)
        {
            return;
        }

        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, commandContext);

        // Process expression <id> <op> <expr> or <expr> <op> <id>.
        async ValueTask<bool> HandleBinaryOperationAsync(IAstNode node, AstTraversal traversal, CancellationToken ct)
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
            if (binaryOperationExpressionNode.MatchType(out IdentifierExpressionNode? identifierNode, out ExpressionNode? expressionNode)
                && await TryFindAndAddIdConditionAsync(identifierNode, expressionNode, ct))
            {
                return true;
            }
            if (binaryOperationExpressionNode.MatchType(out expressionNode, out identifierNode)
                && await TryFindAndAddIdConditionAsync(identifierNode, expressionNode, ct))
            {
                return true;
            }

            return false;

            async ValueTask<bool> TryFindAndAddIdConditionAsync(IdentifierExpressionNode? localIdentifierNode,
                ExpressionNode? localExpressionNode, CancellationToken localCancellationToekn)
            {
                if (localIdentifierNode == null)
                {
                    return false;
                }
                // Try to find correspond row input column.
                await makeDelegateVisitor.RunAndReturnAsync(localIdentifierNode, localCancellationToekn); // This call sets InputColumnKey attribute.
                var column = localIdentifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumnKey);
                if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
                {
                    return false;
                }
                var valueFunc = await makeDelegateVisitor.RunAndReturnAsync(localExpressionNode!, localCancellationToekn);
                commandContext.Conditions.TryAddCondition(column, binaryOperationExpressionNode.Operation,
                    new KeyConditionSingleValueGeneratorFunc(valueFunc));
                return true;
            }
        }

        // Process expression <id> BETWEEN <expr> AND <expr>.
        async ValueTask<bool> HandleBetweenOperationAsync(IAstNode node, AstTraversal traversal, CancellationToken ct)
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
            await makeDelegateVisitor.RunAndReturnAsync(identifierNode, ct); // This call sets InputColumnKey attribute.
            var column = identifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumnKey);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var leftValueFunc = await makeDelegateVisitor.RunAndReturnAsync(betweenExpressionNode.Left, ct);
            var rightValueFunc = await makeDelegateVisitor.RunAndReturnAsync(betweenExpressionNode.Right, ct);
            commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.GreaterOrEquals,
                new KeyConditionSingleValueGeneratorFunc(leftValueFunc));
            commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.LessOrEquals,
                new KeyConditionSingleValueGeneratorFunc(rightValueFunc));
            return true;
        }

        async ValueTask<bool> HandleInOperationAsync(IAstNode node, AstTraversal traversal, CancellationToken ct)
        {
            // Get the IN comparision node.
            if (node is not InOperationExpressionNode inOperationExpressionNode)
            {
                return false;
            }
            // Make sure we have id node.
            if (inOperationExpressionNode.ExpressionNode is not IdentifierExpressionNode identifierNode)
            {
                return false;
            }
            // Try to find correspond row input column.
            await makeDelegateVisitor.RunAndReturnAsync(identifierNode, ct); // This call sets InputColumnKey attribute.
            var column = identifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumnKey);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            if (inOperationExpressionNode.InExpressionValuesNodes is InExpressionValuesNode inExpressionValuesNode)
            {
                var values = await Misc_CreateDelegateAsync(inExpressionValuesNode.ValuesNodes, commandContext, ct);
                if (values.Length == 1)
                {
                    commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.In,
                        new KeyConditionSingleValueGeneratorFunc(values[0]));
                    return true;
                }
                else if (values.Length > 1)
                {
                    commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.In,
                        new KeyConditionValueGeneratorArray(values));
                    return true;
                }
            }
            if (inOperationExpressionNode.InExpressionValuesNodes is SelectQueryNode selectQueryNode)
            {
                var iterator = await new SelectPlanner(ExecutionThread)
                    .CreateIteratorAsync(selectQueryNode, commandContext, ct);
                commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.In,
                    new KeyConditionValueGeneratorIterator(iterator));
                return true;
            }
            if (inOperationExpressionNode.InExpressionValuesNodes is IdentifierExpressionNode identifierInNode)
            {
                var identifierNodeAction = await makeDelegateVisitor.RunAndReturnAsync(identifierInNode, ct);
                commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.In,
                    new KeyConditionValueGeneratorVariable(identifierNodeAction));
                return true;
            }
            return false;
        }

        var callbackVisitor = new CallbackDelegateVisitor();
        callbackVisitor.AstTraversal.TypesToIgnore.Add(typeof(SelectQuerySpecificationNode));
        callbackVisitor.AstTraversal.TypesToIgnore.Add(typeof(SelectQueryCombineNode));
        callbackVisitor.Callback = async (node, traversal, ct) =>
        {
            if (await HandleBinaryOperationAsync(node, traversal, ct))
            {
                return;
            }
            if (await HandleBetweenOperationAsync(node, traversal, ct))
            {
                return;
            }
            if (await HandleInOperationAsync(node, traversal, ct))
            {
            }
        };
        await callbackVisitor.RunAsync(predicateNode, cancellationToken);
    }

    /// <summary>
    /// Validate key columns values.
    /// </summary>
    private void QueryContext_ValidateKeyColumnsValues(SelectCommandContext context)
    {
        foreach (var keyCondition in context.GetAllConditionsColumns())
        {
            if (keyCondition.KeyColumn.IsRequired && keyCondition.Conditions.Length < 1)
            {
                var column = keyCondition.RowsInput.Columns[keyCondition.KeyColumn.ColumnIndex];
                throw new QueryMissedCondition(column.FullName, keyCondition.KeyColumn.GetOperations());
            }
        }
    }
}
