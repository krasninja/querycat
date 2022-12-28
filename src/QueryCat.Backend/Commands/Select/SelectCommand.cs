using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// SELECT command.
/// </summary>
public sealed class SelectCommand
{
    public CommandContext Execute(ExecutionThread executionThread, SelectStatementNode selectStatementNode)
    {
        // Create initial empty context for every query.
        new PrepareContextVisitor().Run(selectStatementNode);

        // For every ".. FROM func()" function we create IRowsInput.
        new CreateRowsInputVisitor(executionThread).Run(selectStatementNode);

        // Do some AST transformations.
        new TransformQueryAstVisitor().Run(selectStatementNode);

        // Iterate by select node in pre-order way and create correspond command context.
        new CreateContextVisitor(executionThread).Run(selectStatementNode);

        return selectStatementNode.QueryNode.GetRequiredAttribute<CommandContext>(AstAttributeKeys.ContextKey);
    }
}
