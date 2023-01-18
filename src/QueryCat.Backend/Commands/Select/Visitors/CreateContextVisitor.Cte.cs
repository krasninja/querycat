using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed partial class CreateContextVisitor
{
    private void Cte_PrepareInputList(SelectCommandContext context, SelectQuerySpecificationNode node)
    {
        context.CteList.AddRange(Cte_GetParentList(context));
        if (node.WithNode == null)
        {
            return;
        }

        foreach (var withNode in node.WithNode.WithNodes)
        {
            var processedAsRecursive = false;
            if (node.WithNode.IsRecursive)
            {
                processedAsRecursive = Cte_PrepareInputRecursiveList(context, withNode);
            }

            if (!processedAsRecursive)
            {
                Cte_PrepareInputNonRecursiveList(context, withNode);
                Cte_FixColumnsNames(withNode.ColumnNodes, context.CurrentIterator);
            }
        }
    }

    private void Cte_PrepareInputNonRecursiveList(SelectCommandContext context, SelectWithNode withNode)
    {
        var cteCreateContextVisitor = new CreateContextVisitor(_executionThread);
        cteCreateContextVisitor.Run(withNode.QueryNode);
        var withNodeContext = withNode.QueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var cte = new CommonTableExpression(
            withNode.Name,
            withNodeContext.CurrentIterator);
        context.CteList.Add(cte);
        CreateFinalIterator(withNode.QueryNode);
    }

    private bool Cte_PrepareInputRecursiveList(SelectCommandContext context, SelectWithNode withNode)
    {
        if (withNode.QueryNode is not SelectQueryCombineNode combineNode)
        {
            return false;
        }
        if (combineNode.CombineType != SelectQueryCombineType.Union)
        {
            throw new SemanticException("Recursive query must have UNION [ALL] term.");
        }

        // Prepare and evaluate initial query.
        var cteCreateContextVisitor = new CreateContextVisitor(_executionThread);
        cteCreateContextVisitor.Run(combineNode.LeftQueryNode);
        var initialQueryCommandContext = combineNode.LeftQueryNode
            .GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        Cte_FixColumnsNames(
            withNode.ColumnNodes,
            initialQueryCommandContext.CurrentIterator);

        // Add it into CTE list. Instead of exposing RowsIterator we wrap it into proxy.
        // By switching that proxy to new rows set we evaluate recursive iterator with the new result.
        var proxyRowsIterator = new ProxyRowsIterator(new EmptyIterator(initialQueryCommandContext.CurrentIterator));
        context.CteList.Add(new CommonTableExpression(withNode.Name, proxyRowsIterator));

        // Then prepare iterator for recursive part.
        cteCreateContextVisitor.Run(combineNode.RightQueryNode);
        var recursiveCommandContext = combineNode.RightQueryNode
            .GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        // Final result.
        var totalResult = new RowsFrame(initialQueryCommandContext.CurrentIterator.Columns);
        var totalResultProxy = new ProxyRowsIterator(totalResult);
        var writeRowsIterator = new WriteRowsFrameIterator(totalResult,
            combineNode.IsDistinct ? new DistinctRowsIterator(totalResultProxy) : totalResultProxy);

        // Merge current working frame and recalculate it based on new result.
        var workingFrame = initialQueryCommandContext.CurrentIterator.ToFrame();
        while (!workingFrame.IsEmpty)
        {
            // Append working iterator to the total result.
            totalResultProxy.Set(workingFrame.GetIterator());
            writeRowsIterator.WriteAll();

            // Run the recursive query based on new working rows set.
            proxyRowsIterator.Set(workingFrame.GetIterator());
            workingFrame = recursiveCommandContext.CurrentIterator.ToFrame();
            recursiveCommandContext.CurrentIterator.Reset();
        }

        proxyRowsIterator.Set(totalResult.GetIterator());
        return true;
    }

    private static IEnumerable<CommonTableExpression> Cte_GetParentList(SelectCommandContext context)
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

    private static void Cte_FixColumnsNames(
        IList<SelectColumnsSublistNode> targetColumns,
        IRowsIterator iterator)
    {
        var columns = iterator.Current.Columns;
        for (var columnIndex = 0; columnIndex < targetColumns.Count; columnIndex++)
        {
            if (columns.Length - 1 >= columnIndex
                && targetColumns[columnIndex] is SelectColumnsSublistExpressionNode nameNode
                && nameNode.ExpressionNode is IdentifierExpressionNode idNode)
            {
                columns[columnIndex].Name = idNode.FullName;
            }
        }
    }
}
