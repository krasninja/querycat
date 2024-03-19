using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    internal IExecutionThread<ExecutionOptions> ExecutionThread { get; }

    public SelectPlanner(IExecutionThread<ExecutionOptions> executionThread)
    {
        ExecutionThread = executionThread;
    }

    public IRowsIterator CreateIterator(SelectQueryNode queryNode, SelectCommandContext? parentContext = null)
    {
        if (queryNode.HasAttribute(AstAttributeKeys.ContextKey))
        {
            return queryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey).CurrentIterator;
        }

        if (queryNode is SelectQuerySpecificationNode querySpecificationNode)
        {
            return CreateIteratorInternal(querySpecificationNode, parentContext);
        }
        if (queryNode is SelectQueryCombineNode queryCombineNode)
        {
            return CreateIteratorInternal(queryCombineNode, parentContext);
        }
        throw new InvalidOperationException($"Not supported node type {queryNode.GetType()}.");
    }

    private IRowsIterator CreateIteratorInternal(SelectQuerySpecificationNode node, SelectCommandContext? parentContext = null)
    {
        // FROM.
        var context = Context_Create(node, parentContext);

        // Misc.
        Pipeline_ApplyStatistic(context);
        Pipeline_SubscribeOnErrorsFromInputSources(context);

        // WHERE.
        Pipeline_ApplyFilter(context, node.TableExpressionNode);

        // GROUP BY/HAVING.
        PipelineAggregate_ApplyGrouping(context, node);
        PipelineAggregate_ApplyHaving(context, node.TableExpressionNode?.HavingNode);

        // DISTINCT ON.
        Pipeline_CreateDistinctOnRowsSet(context, node);

        // SELECT.
        Pipeline_ResolveSelectAllStatement(context, node.ColumnsListNode);
        Pipeline_ResolveSelectSourceColumns(context, node);
        Pipeline_AddSelectRowsSet(context, node.ColumnsListNode);

        // WINDOW.
        PipelineWindow_ApplyWindowFunctions(context, node);

        // Fill query context.
        QueryContext_FillQueryContextConditions(context, node);
        QueryContext_ValidateKeyColumnsValues(context);

        // ORDER BY.
        Pipeline_AddRowIdColumn(context, node.ColumnsListNode);
        Pipeline_ApplyOrderBy(context, node.OrderByNode);

        // INTO and SELECT.
        Pipeline_SetOutputFunction(context, node);
        Pipeline_SetSelectRowsSet(context, node.ColumnsListNode);

        // DISTINCT ALL.
        Pipeline_CreateDistinctAllRowsSet(context, node);

        // OFFSET, FETCH.
        Pipeline_ApplyOffsetFetch(context, node.OffsetNode, node.FetchNode);


        // INTO.
        Pipeline_CreateOutput(context, node);

        return context.CurrentIterator;
    }

    private IRowsIterator CreateIteratorInternal(SelectQueryCombineNode node, SelectCommandContext? parentContext = null)
    {
        var context = Context_Create(node, parentContext);
        var leftIterator = CreateIterator(node.LeftQueryNode, parentContext);
        var rightIterator = CreateIterator(node.RightQueryNode, parentContext);
        var combineRowsIterator = new CombineRowsIterator(
            leftIterator,
            rightIterator,
            ConvertCombineType(node.CombineType),
            node.IsDistinct);
        context.SetIterator(combineRowsIterator);

        // Process.
        Pipeline_ApplyOrderBy(context, node.OrderByNode);
        Pipeline_ApplyOffsetFetch(context, node.OffsetNode, node.FetchNode);
        var resultIterator = context.CurrentIterator;
        if (context.HasOutput)
        {
            resultIterator = new ExecuteRowsIterator(resultIterator);
        }

        // Set result. If INTO clause is specified we do not return IRowsIterator outside. Just
        // iterating it we will save rows into target. Otherwise we return it as is.
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
