using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal partial class SpecificationNodeVisitor
{
    private void FillQueryContextConditions(
        SelectQuerySpecificationNode querySpecificationNode,
        SelectCommandContext commandContext)
    {
        // Fill conditions.
        foreach (var context in commandContext.InputQueryContextList)
        {
            FillQueryContextConditions(querySpecificationNode, context, commandContext);
        }

        // Fill "limit". For now we limit only of order is not defined.
        if (querySpecificationNode.OrderByNode == null && querySpecificationNode.FetchNode != null)
        {
            var fetchCount = new SelectCreateDelegateVisitor(ExecutionThread, commandContext)
                .RunAndReturn(querySpecificationNode.FetchNode.CountNode).Invoke().AsInteger;
            foreach (var queryContext in commandContext.InputQueryContextList)
            {
                queryContext.QueryInfo.Limit = fetchCount;
            }
        }
    }

    private void FillQueryContextConditions(
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
            rowsInputContext.QueryInfo.AddCondition(column, binaryOperationExpressionNode.Operation, valueFunc);
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
            rowsInputContext.QueryInfo.AddCondition(column, VariantValue.Operation.GreaterOrEquals, leftValueFunc);
            rowsInputContext.QueryInfo.AddCondition(column, VariantValue.Operation.LessOrEquals, rightValueFunc);
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
            if (inOperationExpressionNode.Expression is not IdentifierExpressionNode identifierNode)
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
            foreach (var inExpressionValue in inOperationExpressionNode.InExpressionValues.Values)
            {
                values.Add(makeDelegateVisitor.RunAndReturn(inExpressionValue));
            }
            rowsInputContext.QueryInfo.AddCondition(column, VariantValue.Operation.In, values.ToArray());
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
}
