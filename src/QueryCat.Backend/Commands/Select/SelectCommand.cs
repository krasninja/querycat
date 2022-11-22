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
        // First we create context for SELECT command by analyzing FROM expression.
        new SelectContextCreator(executionThread).CreateForQuery(selectStatementNode.QueryNode.Queries);

        // Then create query context for remain sub queries.
        new SelectCreateContextVisitor(executionThread).Run(selectStatementNode);

        // Create final execution delegate.
        new SelectBodyNodeVisitor(executionThread).Run(selectStatementNode.QueryNode);

        return selectStatementNode.QueryNode.GetRequiredAttribute<CommandContext>(AstAttributeKeys.ContextKey);
    }
}
