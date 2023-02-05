using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Functions;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    public void Window_ApplyWindowFunctions(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        var windowTargets = querySpecificationNode.ColumnsListNode.ColumnsNodes
            .OfType<SelectColumnsSublistWindowNode>()
            .ToArray();
        if (windowTargets.Length < 1)
        {
            return;
        }

        var list = new List<WindowFunctionInfo>();
        foreach (var windowTarget in windowTargets)
        {
            var partitionFormatters = Array.Empty<IFuncUnit>();

            var existingWindowName = windowTarget.WindowSpecificationNode.ExistingWindowName;
            while (!string.IsNullOrEmpty(existingWindowName))
            {
                if (querySpecificationNode.WindowNode == null)
                {
                    existingWindowName = string.Empty;
                    continue;
                }
                var partitionNode = querySpecificationNode.WindowNode.DefinitionListNodes
                    .Find(n => n.Name.Equals(existingWindowName, StringComparison.OrdinalIgnoreCase));
                if (partitionNode == null)
                {
                    throw new QueryCatException($"Cannot find partition window with name '{existingWindowName}'.");
                }
                existingWindowName = partitionNode.WindowSpecificationNode.ExistingWindowName;
                windowTarget.WindowSpecificationNode.PartitionNode = partitionNode.WindowSpecificationNode.PartitionNode;
            }

            if (windowTarget.WindowSpecificationNode.PartitionNode != null)
            {
                partitionFormatters = windowTarget.WindowSpecificationNode.PartitionNode.ExpressionNodes
                    .Select(exp => Misc_CreateDelegate(exp))
                    .ToArray();
            }
            list.Add(new WindowFunctionInfo(partitionFormatters));
        }

        var windowIterator = new WindowFunctionsRowsIterator(context.CurrentIterator, list);
        context.SetIterator(windowIterator);
    }
}
