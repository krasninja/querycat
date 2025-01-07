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
    private void QueryContext_FillQueryContextConditions(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        // Fill conditions.
        foreach (var inputContext in context.InputQueryContextList)
        {
            QueryContext_FillQueryContextConditions(
                querySpecificationNode.TableExpressionNode?.SearchConditionNode?.ExpressionNode,
                inputContext,
                context);
            foreach (var joinedOnNode in querySpecificationNode.GetAllChildren<SelectTableJoinedOnNode>())
            {
                QueryContext_FillQueryContextConditions(
                    joinedOnNode.SearchConditionNode,
                    inputContext,
                    context);
            }
        }

        // Fill "limit". For now we limit only if order is not defined.
        if (querySpecificationNode.OrderByNode == null)
        {
            if (querySpecificationNode.FetchNode != null)
            {
                var fetchCount = Misc_CreateDelegate(querySpecificationNode.FetchNode.CountNode)
                    .Invoke(ExecutionThread).AsInteger;
                foreach (var queryContext in context.InputQueryContextList)
                {
                    queryContext.QueryInfo.Limit = (queryContext.QueryInfo.Limit ?? 0) + fetchCount;
                }
            }
            if (querySpecificationNode.OffsetNode != null)
            {
                var offsetCount = Misc_CreateDelegate(querySpecificationNode.OffsetNode.CountNode)
                    .Invoke(ExecutionThread).AsInteger;
                foreach (var queryContext in context.InputQueryContextList)
                {
                    queryContext.QueryInfo.Limit = (queryContext.QueryInfo.Limit ?? 0) + offsetCount;
                }
            }
        }
    }

    private void QueryContext_FillQueryContextConditions(
        ExpressionNode? predicateNode,
        SelectInputQueryContext rowsInputContext,
        SelectCommandContext commandContext)
    {
        if (predicateNode == null)
        {
            return;
        }

        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, commandContext);

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
            if (binaryOperationExpressionNode.MatchType(out IdentifierExpressionNode? identifierNode, out ExpressionNode? expressionNode)
                && TryFindAndAddIdCondition(identifierNode, expressionNode))
            {
                return true;
            }
            if (binaryOperationExpressionNode.MatchType(out expressionNode, out identifierNode)
                && TryFindAndAddIdCondition(identifierNode, expressionNode))
            {
                return true;
            }

            return false;

            bool TryFindAndAddIdCondition(IdentifierExpressionNode? localIdentifierNode, ExpressionNode? localExpressionNode)
            {
                if (localIdentifierNode == null)
                {
                    return false;
                }
                // Try to find correspond row input column.
                makeDelegateVisitor.RunAndReturn(localIdentifierNode); // This call sets InputColumnKey attribute.
                var column = localIdentifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumnKey);
                if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
                {
                    return false;
                }
                var valueFunc = makeDelegateVisitor.RunAndReturn(localExpressionNode!);
                commandContext.Conditions.TryAddCondition(column, binaryOperationExpressionNode.Operation,
                    new KeyConditionSingleValueGeneratorFunc(valueFunc));
                return true;
            }
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
            makeDelegateVisitor.RunAndReturn(identifierNode); // This call sets InputColumnKey attribute.
            var column = identifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumnKey);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var leftValueFunc = makeDelegateVisitor.RunAndReturn(betweenExpressionNode.Left);
            var rightValueFunc = makeDelegateVisitor.RunAndReturn(betweenExpressionNode.Right);
            commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.GreaterOrEquals,
                new KeyConditionSingleValueGeneratorFunc(leftValueFunc));
            commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.LessOrEquals,
                new KeyConditionSingleValueGeneratorFunc(rightValueFunc));
            return true;
        }

        bool HandleInOperation(IAstNode node, AstTraversal traversal)
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
            makeDelegateVisitor.RunAndReturn(identifierNode); // This call sets InputColumnKey attribute.
            var column = identifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumnKey);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            if (inOperationExpressionNode.InExpressionValuesNodes is InExpressionValuesNode inExpressionValuesNode)
            {
                var values = inExpressionValuesNode.ValuesNodes.Select(n => makeDelegateVisitor.RunAndReturn(n)).ToArray();
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
                var iterator = new SelectPlanner(ExecutionThread).CreateIterator(selectQueryNode, commandContext);
                commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.In,
                    new KeyConditionValueGeneratorIterator(iterator));
                return true;
            }
            if (inOperationExpressionNode.InExpressionValuesNodes is IdentifierExpressionNode identifierInNode)
            {
                var identifierNodeAction = makeDelegateVisitor.RunAndReturn(identifierInNode);
                commandContext.Conditions.TryAddCondition(column, VariantValue.Operation.In,
                    new KeyConditionValueGeneratorVariable(identifierNodeAction));
                return true;
            }
            return false;
        }

        var callbackVisitor = new CallbackDelegateVisitor();
        callbackVisitor.AstTraversal.TypesToIgnore.Add(typeof(SelectQuerySpecificationNode));
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
            if (HandleInOperation(node, traversal))
            {
            }
        };
        callbackVisitor.Run(predicateNode);
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
