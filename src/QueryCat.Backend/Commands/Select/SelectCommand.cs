using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// SELECT command.
/// </summary>
internal sealed class SelectCommand : ICommand
{
    /// <inheritdoc />
    public CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var selectQueryNode = (SelectQueryNode)node.RootNode;

        // Iterate by select node in pre-order way and create correspond command context.
        new SelectPlanner(executionThread).CreateIterator(selectQueryNode);
        var context = selectQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        // Apply row_number column.
        ApplyRowIdIterator(executionThread, context);

        return new SelectCommandHandler(context);
    }

    private void ApplyRowIdIterator(ExecutionThread executionThread, SelectCommandContext bodyContext)
    {
        var isSubQuery = bodyContext.Parent != null;
        var resultIterator = bodyContext.CurrentIterator;
        if (executionThread.Options.AddRowNumberColumn
            && !isSubQuery
            && resultIterator.GetColumnIndexByName(RowIdRowsIterator.ColumName) == -1)
        {
            resultIterator = new RowIdRowsIterator(resultIterator);
        }
        bodyContext.SetIterator(resultIterator);
    }
}
