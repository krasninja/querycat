using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// The visitor is po process <see cref="SelectQueryExpressionBodyNode" /> nodes only in post order way.
/// </summary>
internal sealed class SelectBodyNodeVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;
    private readonly AstTraversal _astTraversal;

    public SelectBodyNodeVisitor(ExecutionThread executionThread)
    {
        _executionThread = executionThread;
        this._astTraversal = new AstTraversal(this);
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        _astTraversal.PostOrder(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQueryExpressionBodyNode node)
    {
        var selectQueryBodyVisitor = new SelectSpecificationNodeVisitor(_executionThread);
        selectQueryBodyVisitor.Run(node.Queries);

        // Add all iterators and merge them into "combine" iterator.
        var combineRowsIterator = new CombineRowsIterator();
        var hasOutputInQuery = false;
        foreach (var queryNode in node.Queries)
        {
            var queryContext = queryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ResultKey);
            if (queryContext.HasOutput)
            {
                hasOutputInQuery = true;
            }
            combineRowsIterator.AddRowsIterator(queryContext.CurrentIterator);
        }

        // Format final result iterator.
        var resultIterator = combineRowsIterator.RowIterators.Count == 1
            ? combineRowsIterator.RowIterators.First()
            : combineRowsIterator;
        if (_executionThread.Options.AddRowNumberColumn)
        {
            resultIterator = new RowIdRowsIterator(resultIterator);
        }

        // Set result. If INTO clause is specified we do not return IRowsIterator outside. Just
        // iterating it we will save rows into target. Otherwise we return it as is.
        node.SetAttribute(AstAttributeKeys.ResultKey, resultIterator);
        if (hasOutputInQuery)
        {
            node.SetFunc(() =>
            {
                resultIterator.MoveToEnd();
                return VariantValue.Null;
            });
        }
        else
        {
            node.SetFunc(() => VariantValue.CreateFromObject(resultIterator));
        }
    }
}
