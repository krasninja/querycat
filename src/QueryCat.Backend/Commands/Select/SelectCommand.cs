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
        // For every ".. FROM func()" function we create IRowsInput.
        new TableRowsInputVisitor(executionThread).Run(selectStatementNode);

        // Create command context for every FROM clause.
        new CreateContextVisitor(executionThread).Run(selectStatementNode.QueryNode);

        // Create final execution delegate.
        new SpecificationNodeVisitor(executionThread).Run(selectStatementNode);

        return selectStatementNode.QueryNode.GetRequiredAttribute<CommandContext>(AstAttributeKeys.ContextKey);
    }
}
