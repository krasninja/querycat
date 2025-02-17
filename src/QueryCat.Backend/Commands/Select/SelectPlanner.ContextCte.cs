using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    private async Task ContextCte_PrepareInputListAsync(SelectCommandContext context, SelectQuerySpecificationNode node,
        CancellationToken cancellationToken)
    {
        context.CteList.AddRange(ContextCte_GetParentList(context));
        if (node.WithNode == null)
        {
            return;
        }

        foreach (var withNode in node.WithNode.WithNodes)
        {
            var processedAsRecursive = false;
            if (node.WithNode.IsRecursive)
            {
                processedAsRecursive = await ContextCte_PrepareInputRecursiveListAsync(context, withNode, cancellationToken);
            }

            if (!processedAsRecursive)
            {
                await ContextCte_PrepareInputNonRecursiveListAsync(context, withNode, cancellationToken);
            }
        }
    }

    private async Task ContextCte_PrepareInputNonRecursiveListAsync(SelectCommandContext context, SelectWithNode withNode,
        CancellationToken cancellationToken)
    {
        var rowsIterator = await CreateIteratorAsync(withNode.QueryNode, context, cancellationToken);
        var cte = new CommonTableExpression(
            withNode.Name,
            rowsIterator);
        context.CteList.Add(cte);
    }

    private async Task<bool> ContextCte_PrepareInputRecursiveListAsync(SelectCommandContext context, SelectWithNode withNode, CancellationToken cancellationToken)
    {
        if (withNode.QueryNode is not SelectQueryCombineNode combineNode)
        {
            return false;
        }
        if (combineNode.CombineType != SelectQueryCombineType.Union)
        {
            throw new SemanticException(Resources.Errors.RecursiveMustHaveUnion);
        }

        // Prepare and evaluate initial query.
        var leftIterator = await CreateIteratorAsync(combineNode.LeftQueryNode, context, cancellationToken);
        var initialQueryCommandContext = combineNode.LeftQueryNode
            .GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        ContextCte_FixColumnsNames(
            withNode.ColumnNodes,
            leftIterator);

        // Add it into CTE list. Instead of exposing RowsIterator we wrap it into proxy.
        // By switching that proxy to new rows set we evaluate recursive iterator with the new result.
        var proxyRowsIterator = new ProxyRowsIterator(new EmptyIterator(initialQueryCommandContext.CurrentIterator));
        context.CteList.Add(new CommonTableExpression(withNode.Name, proxyRowsIterator));

        // Then prepare iterator for recursive part.
        var rightIterator = await CreateIteratorAsync(combineNode.RightQueryNode, context, cancellationToken);

        // Final result.
        var totalResult = new RowsFrame(initialQueryCommandContext.CurrentIterator.Columns);
        var totalResultProxy = new ProxyRowsIterator(totalResult);
        var writeRowsIterator = new WriteRowsFrameIterator(totalResult,
            combineNode.IsDistinct ? new DistinctRowsIteratorIterator(ExecutionThread, totalResultProxy) : totalResultProxy);

        // Merge current working frame and recalculate it based on new result.
        var workingFrame = await initialQueryCommandContext.CurrentIterator.ToFrameAsync(cancellationToken);
        while (!workingFrame.IsEmpty)
        {
            // Append working iterator to the total result.
            totalResultProxy.Set(workingFrame.GetIterator());
            await writeRowsIterator.WriteAllAsync(cancellationToken);

            // Run the recursive query based on new working rows set.
            proxyRowsIterator.Set(workingFrame.GetIterator());
            var newFrame = await rightIterator.ToFrameAsync(cancellationToken);
            workingFrame = newFrame;
            await rightIterator.ResetAsync(cancellationToken);
        }

        proxyRowsIterator.Set(totalResult.GetIterator());
        return true;
    }

    private static IEnumerable<CommonTableExpression> ContextCte_GetParentList(SelectCommandContext context)
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

    private static void ContextCte_FixColumnsNames(
        IList<SelectColumnsSublistNode> targetColumns,
        IRowsIterator iterator)
    {
        var columns = iterator.Columns;
        for (var columnIndex = 0; columnIndex < targetColumns.Count && columns.Length - 1 >= columnIndex; columnIndex++)
        {
            if (targetColumns[columnIndex] is SelectColumnsSublistExpressionNode { ExpressionNode: IdentifierExpressionNode idNode })
            {
                columns[columnIndex].Name = idNode.FullName;
            }
        }
    }
}
