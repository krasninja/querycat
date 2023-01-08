using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;

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

        foreach (var withNodeItem in node.WithNode.Nodes)
        {
            if (node.WithNode.IsRecursive)
            {
                var processedAsRecursive = Cte_PrepareInputRecursiveList(context, withNodeItem);
                if (processedAsRecursive)
                {
                    continue;
                }
            }

            Cte_PrepareInputNonRecursiveList(context, withNodeItem);
        }
    }

    private void Cte_PrepareInputNonRecursiveList(SelectCommandContext context, SelectWithNode withNode)
    {
        var cteCreateContextVisitor = new CreateContextVisitor(_executionThread);
        cteCreateContextVisitor.Run(withNode.QueryNode);
        var cte = new CommonTableExpression(
            withNode.Name,
            withNode.QueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey));
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

        return false;
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
}
