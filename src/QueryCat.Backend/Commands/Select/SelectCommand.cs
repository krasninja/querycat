using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// SELECT command.
/// </summary>
public sealed class SelectCommand
{
    public Func<VariantValue> Execute(ExecutionThread executionThread, SelectStatementNode selectStatementNode)
    {
        // First we create context for SELECT command by analyzing FROM expression.
        new SelectContextCreator(executionThread).CreateForQuery(selectStatementNode.QueryNode.Queries);

        // Then create query context for remain sub queries.
        new SelectQueryCreateContextVisitor(executionThread).Run(selectStatementNode);

        // Create final execution delegate.
        new SelectQueryBodyNodeVisitor(executionThread).Run(selectStatementNode.QueryNode);

        return selectStatementNode.QueryNode.GetFunc();
    }
}
