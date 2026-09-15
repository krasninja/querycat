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
            windowTarget.WindowSpecificationNode = PipelineWindow_ResolveWindowSpecification(
                windowTarget.WindowSpecificationNode, querySpecificationNode.WindowNode);

            var columnInfo = context.ColumnsInfoContainer.Columns.First(c => c.RelatedSelectSublistNode == windowTarget);
            var iteratorColumnIndex = Array.IndexOf(context.CurrentIterator.Columns, columnInfo.Column);
            var windowFunctionInfo = await PipelineWindow_PrepareWindowFunctionInfoAsync(iteratorColumnIndex, windowTarget,
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
    /// Resolve the window specification that references the existing window by name. For example,
    /// "OVER (w ORDER BY id)". The partition clause is taken from the referenced window, and the order clause
    /// can be defined either by the referenced window or by the current specification.
    /// </summary>
    /// <param name="windowSpecificationNode">Window specification to resolve.</param>
    /// <param name="windowNode">WINDOW clause with named windows definitions.</param>
    /// <param name="visitedWindowNames">Already resolved window names, used to detect circular references.</param>
    /// <returns>Resolved window specification without window name reference.</returns>
    private static SelectWindowSpecificationNode PipelineWindow_ResolveWindowSpecification(
        SelectWindowSpecificationNode windowSpecificationNode,
        SelectWindowNode? windowNode,
        HashSet<string>? visitedWindowNames = null)
    {
        var existingWindowName = windowSpecificationNode.ExistingWindowName;
        if (string.IsNullOrEmpty(existingWindowName))
        {
            return windowSpecificationNode;
        }
        if (windowSpecificationNode.PartitionNode != null)
        {
            throw new SemanticException(string.Format(Resources.Errors.CannotOverrideWindowPartition, existingWindowName));
        }

        visitedWindowNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!visitedWindowNames.Add(existingWindowName))
        {
            throw new SemanticException(string.Format(Resources.Errors.WindowCircularReference, existingWindowName));
        }
        var definitionNode = windowNode?.DefinitionListNodes
            .Find(n => n.Name.Equals(existingWindowName, StringComparison.OrdinalIgnoreCase));
        if (definitionNode == null)
        {
            throw new SemanticException(string.Format(Resources.Errors.CannotFindPartition, existingWindowName));
        }

        var baseWindowSpecificationNode = PipelineWindow_ResolveWindowSpecification(
            definitionNode.WindowSpecificationNode, windowNode, visitedWindowNames);
        if (windowSpecificationNode.OrderNode != null && baseWindowSpecificationNode.OrderNode != null)
        {
            throw new SemanticException(string.Format(Resources.Errors.CannotOverrideWindowOrder, existingWindowName));
        }
        return new SelectWindowSpecificationNode(
            baseWindowSpecificationNode.PartitionNode,
            windowSpecificationNode.OrderNode ?? baseWindowSpecificationNode.OrderNode);
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
