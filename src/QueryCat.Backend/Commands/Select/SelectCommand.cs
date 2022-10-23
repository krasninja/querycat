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
        new SelectQueryMakeInputVisitor(executionThread).Run(selectStatementNode);
        new SelectQueryBodyVisitor(executionThread).Run(selectStatementNode);
        return selectStatementNode.QueryNode.GetFunc();
    }
}
