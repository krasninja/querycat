using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// SELECT command.
/// </summary>
internal sealed class SelectCommand : ICommand
{
    /// <inheritdoc />
    public IFuncUnit CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var selectQueryNode = (SelectQueryNode)node.RootNode;

        // Iterate by select node in pre-order way and create correspond command context.
        new SelectPlanner(executionThread).CreateIterator(selectQueryNode);
        var context = selectQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        return new SelectCommandHandler(context);
    }
}
