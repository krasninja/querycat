using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.Update;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend.Commands.Update;

internal class UpdateCommand : ICommand
{
    /// <inheritdoc />
    public CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var insertNode = (UpdateNode)node.RootNode;

        // Format SELECT statement and use it for further iterations.
        var selectNode = new SelectQuerySpecificationNode(
            new SelectColumnsListNode(insertNode.SetNodes.Select(n => new SelectColumnsSublistExpressionNode(n.SetTargetNode))
        ));
        selectNode.TableExpressionNode =
            new SelectTableExpressionNode(
                    new SelectTableReferenceListNode(insertNode.TargetExpressionNode));
        selectNode.TableExpressionNode.SearchConditionNode = insertNode.SearchConditionNode;
        new SelectPlanner(executionThread).CreateIterator(selectNode);
        var context = selectNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        // Evaluate setters.
        if (context.RowsInputIterator?.RowsInput is not IRowsInputUpdate rowsInput)
        {
            throw new QueryCatException("Rows input must be updatable.");
        }
        var setters = new List<UpdateSetter>();
        var createDelegateVisitor = new SelectCreateDelegateVisitor(executionThread, context);
        foreach (var setNode in insertNode.SetNodes)
        {
            var columnIndex = rowsInput.GetColumnIndexByName(setNode.SetTargetNode.Name,
                setNode.SetTargetNode.SourceName);
            if (columnIndex < 0)
            {
                throw new QueryCatException($"Cannot find column '{setNode.SetTargetNode.FullName}'.");
            }
            var func = createDelegateVisitor.RunAndReturn(setNode.SetSourceNode);
            setters.Add(new UpdateSetter(columnIndex, func));
        }

        return new UpdateCommandHandler(context, setters.ToArray());
    }
}
