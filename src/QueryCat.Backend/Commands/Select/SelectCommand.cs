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
        // Create command context for every FROM clause.
        new CreateContextVisitor(executionThread).Run(selectStatementNode.QueryNode);

        // Create final execution delegate.
        new BodyNodeVisitor(executionThread).Run(selectStatementNode.QueryNode);

        return selectStatementNode.QueryNode.GetRequiredAttribute<CommandContext>(AstAttributeKeys.ContextKey);
    }
}
