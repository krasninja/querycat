using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed partial class SetIteratorVisitor
{
    public void Window_ApplyWindowFunctions(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        ResolveNodesTypes(
            new IAstNode?[] { querySpecificationNode.WindowNode },
            context);
        var windowTargets = querySpecificationNode.ColumnsListNode.ColumnsNodes
            .OfType<SelectColumnsSublistWindowNode>()
            .ToArray();
        if (windowTargets.Length < 1)
        {
            return;
        }

        context.SetIterator(new WindowFunctionsRowsIterator(context.CurrentIterator));
    }
}
