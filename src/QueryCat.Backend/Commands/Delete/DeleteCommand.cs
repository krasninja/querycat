using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Delete;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Delete;

internal sealed class DeleteCommand : ICommand
{
    /// <inheritdoc />
    public async Task<IFuncUnit> CreateHandlerAsync(IExecutionThread<ExecutionOptions> executionThread, StatementNode node,
        CancellationToken cancellationToken = default)
    {
        if (executionThread.Options.SafeMode)
        {
            throw new SafeModeException();
        }

        var deleteNode = (DeleteNode)node.RootNode;

        // Format SELECT statement and use it for further iterations.
        var selectNode = new SelectQuerySpecificationNode(new SelectColumnsListNode(new SelectColumnsSublistAll()));
        selectNode.TableExpressionNode =
            new SelectTableNode(
                new SelectTableReferenceListNode(deleteNode.DeleteTargetNode));
        selectNode.TableExpressionNode.SearchConditionNode = deleteNode.SearchConditionNode;
        await new SelectPlanner(executionThread).CreateIteratorAsync(selectNode, cancellationToken: cancellationToken);
        var context = selectNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        // Get output source.
        var input = deleteNode.DeleteTargetNode.GetRequiredAttribute<IRowsInputDelete>(AstAttributeKeys.RowsInputKey);

        return new DeleteCommandHandler(context, input);
    }
}
