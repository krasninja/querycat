using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// SELECT command.
/// </summary>
public sealed class SelectCommand : ICommand
{
    /// <inheritdoc />
    public CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var selectQueryNode = (SelectQueryNode)node.RootNode;

        // Create initial empty context for every query.
        new PrepareContextVisitor().Run(selectQueryNode);

        // Do some AST transformations.
        new TransformQueryAstVisitor().Run(selectQueryNode);

        // Iterate by select node in pre-order way and create correspond command context.
        new CreateContextVisitor(executionThread).Run(selectQueryNode);

        var context = selectQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        return new SelectCommandHandler(context);
    }
}
