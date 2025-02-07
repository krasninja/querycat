using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    internal IExecutionThread<ExecutionOptions> ExecutionThread { get; }

    private readonly ResolveTypesVisitor _resolveTypesVisitor;

    public SelectPlanner(IExecutionThread<ExecutionOptions> executionThread, ResolveTypesVisitor resolveTypesVisitor)
    {
        ExecutionThread = executionThread;
        _resolveTypesVisitor = resolveTypesVisitor;
    }

    public SelectPlanner(IExecutionThread<ExecutionOptions> executionThread)
        : this(executionThread, new ResolveTypesVisitor(executionThread))
    {
    }

    public async Task<IRowsIterator> CreateIteratorAsync(
        SelectQueryNode queryNode,
        SelectCommandContext? parentContext = null,
        CancellationToken cancellationToken = default)
    {
        if (queryNode.HasAttribute(AstAttributeKeys.ContextKey))
        {
            var iterator = queryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey).CurrentIterator;
            return iterator;
        }

        if (queryNode is SelectQuerySpecificationNode querySpecificationNode)
        {
            return await CreateIteratorInternalAsync(querySpecificationNode, parentContext, cancellationToken);
        }
        if (queryNode is SelectQueryCombineNode queryCombineNode)
        {
            return await CreateIteratorInternalAsync(queryCombineNode, parentContext, cancellationToken);
        }
        throw new InvalidOperationException(string.Format(Resources.Errors.NotSupportedNodeType, queryNode.GetType()));
    }

    private async Task<IRowsIterator> CreateIteratorInternalAsync(
        SelectQuerySpecificationNode node,
        SelectCommandContext? parentContext = null,
        CancellationToken cancellationToken = default)
    {
        // FROM.
        var context = await Context_CreateAsync(node, parentContext, cancellationToken);

        // Misc.
        Pipeline_ApplyStatistic(context);
        Pipeline_SubscribeOnErrorsFromInputSources(context);

        // WHERE.
        await Pipeline_ApplyFilterAsync(context, node.TableExpressionNode, cancellationToken);

        // GROUP BY/HAVING.
        await PipelineAggregate_ApplyGroupingAsync(context, node, cancellationToken);
        await PipelineAggregate_ApplyHavingAsync(context, node.TableExpressionNode?.HavingNode, cancellationToken);

        // DISTINCT ON.
        await Pipeline_CreateDistinctOnRowsSetAsync(context, node, cancellationToken);

        // SELECT.
        Pipeline_ResolveSelectAllStatement(context, node.ColumnsListNode);
        Pipeline_ResolveSelectSourceColumns(context, node);
        await Pipeline_AddSelectRowsSetAsync(context, node.ColumnsListNode, node.ExceptIdentifiersNode, cancellationToken);

        // WINDOW.
        await PipelineWindow_ApplyWindowFunctionsAsync(context, node, cancellationToken);

        // Fill query context.
        QueryContext_ValidateKeyColumnsValues(context);

        // ORDER BY.
        Pipeline_AddRowIdColumn(context, node.ColumnsListNode);
        await Pipeline_ApplyOrderByAsync(context, node.OrderByNode, cancellationToken);

        // INTO and SELECT.
        await Pipeline_SetOutputFunctionAsync(context, node, cancellationToken);
        Pipeline_SetSelectRowsSet(context, node.ColumnsListNode);

        // DISTINCT ALL.
        Pipeline_CreateDistinctAllRowsSet(context, node);

        // OFFSET, FETCH.
        await Pipeline_ApplyOffsetFetchAsync(context, node.OffsetNode, node.FetchNode, cancellationToken);

        // INTO.
        Pipeline_CreateOutput(context, node);

        return context.CurrentIterator;
    }

    private async Task<IRowsIterator> CreateIteratorInternalAsync(
        SelectQueryCombineNode node,
        SelectCommandContext? parentContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = await Context_CreateAsync(node, parentContext, cancellationToken);
        var leftIterator = await CreateIteratorAsync(node.LeftQueryNode, parentContext, cancellationToken);
        var rightIterator = await CreateIteratorAsync(node.RightQueryNode, parentContext, cancellationToken);
        var combineRowsIterator = new CombineRowsIterator(
            leftIterator,
            rightIterator,
            ConvertCombineType(node.CombineType),
            node.IsDistinct);
        context.SetIterator(combineRowsIterator);

        // Process.
        await Pipeline_ApplyOrderByAsync(context, node.OrderByNode, cancellationToken);
        await Pipeline_ApplyOffsetFetchAsync(context, node.OffsetNode, node.FetchNode, cancellationToken);
        var resultIterator = context.CurrentIterator;
        if (context.HasOutput)
        {
            resultIterator = new ExecuteRowsIterator(resultIterator);
        }

        // Set result. If INTO clause is specified we do not return IRowsIterator outside. Just
        // iterating it we will save rows into target. Otherwise, we return it as is.
        node.SetAttribute(AstAttributeKeys.ResultKey, resultIterator);

        return context.CurrentIterator;

        CombineType ConvertCombineType(SelectQueryCombineType combineType) => combineType switch
        {
            SelectQueryCombineType.Except => CombineType.Except,
            SelectQueryCombineType.Intersect => CombineType.Intersect,
            SelectQueryCombineType.Union => CombineType.Union,
            _ => throw new ArgumentException(string.Format(Resources.Errors.NotImplemented, combineType), nameof(combineType)),
        };
    }
}
