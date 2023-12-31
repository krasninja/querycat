using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

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
            QueryContext_FillQueryContextConditions(querySpecificationNode, inputContext, context);
        }

        // Fill "limit". For now we limit only if order is not defined.
        if (querySpecificationNode.OrderByNode == null)
        {
            if (querySpecificationNode.FetchNode != null)
            {
                var fetchCount = Misc_CreateDelegate(querySpecificationNode.FetchNode.CountNode)
                    .Invoke().AsInteger;
                foreach (var queryContext in context.InputQueryContextList)
                {
                    queryContext.QueryInfo.Limit = (queryContext.QueryInfo.Limit ?? 0) + fetchCount;
                }
            }
            if (querySpecificationNode.OffsetNode != null)
            {
                var offsetCount = Misc_CreateDelegate(querySpecificationNode.OffsetNode.CountNode)
                    .Invoke().AsInteger;
                foreach (var queryContext in context.InputQueryContextList)
                {
                    queryContext.QueryInfo.Limit = (queryContext.QueryInfo.Limit ?? 0) + offsetCount;
                }
            }
        }
    }

    private void QueryContext_FillQueryContextConditions(
        SelectQuerySpecificationNode querySpecificationNode,
        SelectInputQueryContext rowsInputContext,
        SelectCommandContext commandContext)
    {
        var searchNode = querySpecificationNode.TableExpressionNode?.SearchConditionNode;
        if (searchNode == null)
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
            if (!binaryOperationExpressionNode.MatchType(out IdentifierExpressionNode? identifierNode, out ExpressionNode? expressionNode))
            {
                return false;
            }
            // Try to find correspond row input column.
            var column = identifierNode!.GetAttribute<Column>(AstAttributeKeys.InputColumnKey);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var valueFunc = makeDelegateVisitor.RunAndReturn(expressionNode!);
            commandContext.Conditions.AddCondition(column, binaryOperationExpressionNode.Operation, valueFunc);
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
            var column = identifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumnKey);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var leftValueFunc = makeDelegateVisitor.RunAndReturn(betweenExpressionNode.Left);
            var rightValueFunc = makeDelegateVisitor.RunAndReturn(betweenExpressionNode.Right);
            commandContext.Conditions.AddCondition(column, VariantValue.Operation.GreaterOrEquals, leftValueFunc);
            commandContext.Conditions.AddCondition(column, VariantValue.Operation.LessOrEquals, rightValueFunc);
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
            var column = identifierNode.GetAttribute<Column>(AstAttributeKeys.InputColumnKey);
            if (column == null || rowsInputContext.RowsInput.GetColumnIndex(column) < 0)
            {
                return false;
            }
            var values = new List<IFuncUnit>();
            if (inOperationExpressionNode.InExpressionValuesNodes is not InExpressionValuesNode inExpressionValuesNode)
            {
                return false;
            }
            foreach (var inExpressionValue in inExpressionValuesNode.ValuesNodes)
            {
                if (inExpressionValue is SelectQueryNode)
                {
                    continue;
                }
                values.Add(makeDelegateVisitor.RunAndReturn(inExpressionValue));
            }
            if (!values.Any())
            {
                return false;
            }
            commandContext.Conditions.AddCondition(column, VariantValue.Operation.In, values.ToArray());
            return true;
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
        callbackVisitor.Run(searchNode);
    }

    /// <summary>
    /// Validate key columns values.
    /// </summary>
    private void QueryContext_ValidateKeyColumnsValues(SelectCommandContext context)
    {
        foreach (var keyCondition in context.GetConditionsColumns())
        {
            if (keyCondition.KeyColumn.IsRequired && keyCondition.Conditions.Length < 1)
            {
                throw new QueryContextMissedCondition(keyCondition.KeyColumn.ColumnName, keyCondition.KeyColumn.Operations);
            }
        }
    }

    private void QueryContext_SetKeyColumns(SelectCommandContext context)
    {
        var rowsInputWithKeys = context.Inputs.Select(i => i.RowsInput).OfType<IRowsInputKeys>().ToArray();
        if (!rowsInputWithKeys.Any())
        {
            return;
        }
        var keysColumns = rowsInputWithKeys.SelectMany(i => i.GetKeyColumns());
        if (!keysColumns.Any())
        {
            return;
        }

        var setKeysColumnsIterator = new SetKeyColumnsRowsIterator(context.CurrentIterator,
            context.GetConditionsColumns().ToArray());
        context.SetIterator(setKeysColumnsIterator);
    }
}
