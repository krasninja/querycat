using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Core;
using QueryCat.Backend.Indexes;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    private async Task PipelineWindow_ApplyWindowFunctionsAsync(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode,
        CancellationToken cancellationToken)
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

            var windowFunctionInfo = await PipelineWindow_PrepareWindowFunctionInfoAsync(columnIndex, windowTarget,
                context, cancellationToken);
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

    private async Task<WindowFunctionInfo> PipelineWindow_PrepareWindowFunctionInfoAsync(
        int columnIndex,
        SelectColumnsSublistWindowNode windowTarget,
        SelectCommandContext context,
        CancellationToken cancellationToken)
    {
        var partitionFormatters = Array.Empty<IFuncUnit>();
        if (windowTarget.WindowSpecificationNode.PartitionNode != null)
        {
            partitionFormatters = await Misc_CreateDelegateAsync(
                windowTarget.WindowSpecificationNode.PartitionNode.ExpressionNodes, context, cancellationToken);
        }

        var orderFunctions = Array.Empty<IFuncUnit>();
        var orderData = Array.Empty<OrderColumnData>();
        if (windowTarget.WindowSpecificationNode.OrderNode != null)
        {
            orderFunctions = await Misc_CreateDelegateAsync(
                windowTarget.WindowSpecificationNode.OrderNode.OrderBySpecificationNodes.Select(n => n.ExpressionNode),
                context,
                cancellationToken);
            orderData = windowTarget.WindowSpecificationNode.OrderNode.OrderBySpecificationNodes
                .Select((n, i) => new OrderColumnData(
                    i,
                    Pipeline_ConvertDirection(n.Order),
                    Pipeline_ConvertNullOrder(n.NullOrder)
                ))
                .ToArray();
        }

        var aggregateFunctionArguments = await Misc_CreateDelegateAsync(
            windowTarget.AggregateFunctionNode.Arguments, context, cancellationToken);
        var aggregateTarget = windowTarget.AggregateFunctionNode
            .GetRequiredAttribute<AggregateTarget>(AstAttributeKeys.AggregateFunctionKey);

        return new WindowFunctionInfo(columnIndex, partitionFormatters,
            orderFunctions, orderData, aggregateFunctionArguments, aggregateTarget);
    }
}
