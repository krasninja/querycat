using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Core;
using QueryCat.Backend.Indexes;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    private void PipelineWindow_ApplyWindowFunctions(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        var windowDataList = new List<WindowFunctionInfo>();
        for (var columnIndex = 0; columnIndex < querySpecificationNode.ColumnsListNode.ColumnsNodes.Count; columnIndex++)
        {
            var windowTarget = querySpecificationNode.ColumnsListNode.ColumnsNodes[columnIndex] as SelectColumnsSublistWindowNode;
            if (windowTarget == null)
            {
                continue;
            }
            windowTarget.WindowSpecificationNode =
                PipelineWindow_GetPartitionNode(windowTarget, querySpecificationNode.WindowNode);

            var windowFunctionInfo = PipelineWindow_PrepareWindowFunctionInfo(columnIndex, windowTarget, context);
            windowDataList.Add(windowFunctionInfo);
        }
        if (windowDataList.Count < 1)
        {
            return;
        }

        // Create final context.
        var windowIterator = new WindowFunctionsRowsIterator(ExecutionThread, context.CurrentIterator, windowDataList);
        context.SetIterator(windowIterator);
    }

    /// <summary>
    /// Find for partition clause. It can be in case if in SELECT there is just a name reference.
    /// </summary>
    private SelectWindowSpecificationNode PipelineWindow_GetPartitionNode(
        SelectColumnsSublistWindowNode windowTarget,
        SelectWindowNode? windowNode)
    {
        var windowSpecificationNode = windowTarget.WindowSpecificationNode;
        if (windowNode != null)
        {
            var existingWindowName = windowTarget.WindowSpecificationNode.ExistingWindowName;
            while (!string.IsNullOrEmpty(existingWindowName))
            {
                var partitionNode = windowNode.DefinitionListNodes
                    .Find(n => n.Name.Equals(existingWindowName, StringComparison.OrdinalIgnoreCase));
                if (partitionNode == null)
                {
                    throw new QueryCatException(string.Format(Resources.Errors.CannotFindPartition, existingWindowName));
                }
                existingWindowName = partitionNode.WindowSpecificationNode.ExistingWindowName;
                windowSpecificationNode = partitionNode.WindowSpecificationNode;
            }
        }
        return windowSpecificationNode;
    }

    private WindowFunctionInfo PipelineWindow_PrepareWindowFunctionInfo(
        int columnIndex,
        SelectColumnsSublistWindowNode windowTarget,
        SelectCommandContext context)
    {
        var partitionFormatters = Array.Empty<IFuncUnit>();
        if (windowTarget.WindowSpecificationNode.PartitionNode != null)
        {
            partitionFormatters = windowTarget.WindowSpecificationNode.PartitionNode.ExpressionNodes
                .Select(exp => Misc_CreateDelegate(exp, context))
                .ToArray();
        }

        var orderFunctions = Array.Empty<IFuncUnit>();
        var orderData = Array.Empty<OrderColumnData>();
        if (windowTarget.WindowSpecificationNode.OrderNode != null)
        {
            orderFunctions = windowTarget.WindowSpecificationNode.OrderNode.OrderBySpecificationNodes
                .Select(exp => Misc_CreateDelegate(exp.ExpressionNode, context))
                .ToArray();
            orderData = windowTarget.WindowSpecificationNode.OrderNode.OrderBySpecificationNodes
                .Select((n, i) => new OrderColumnData(
                    i,
                    Pipeline_ConvertDirection(n.Order),
                    Pipeline_ConvertNullOrder(n.NullOrder)
                ))
                .ToArray();
        }

        var aggregateFunctionArguments = windowTarget.AggregateFunctionNode.Arguments
            .Select(a => Misc_CreateDelegate(a.ExpressionValueNode, context))
            .ToArray();
        var aggregateTarget = windowTarget.AggregateFunctionNode
            .GetRequiredAttribute<AggregateTarget>(AstAttributeKeys.AggregateFunctionKey);

        return new WindowFunctionInfo(columnIndex, partitionFormatters,
            orderFunctions, orderData, aggregateFunctionArguments, aggregateTarget);
    }
}
