using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Visit only program top-level building blocks (statements) and generate execution delegate.
/// </summary>
public sealed class StatementsVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;

    public CommandContext CommandContext { get; private set; } = EmptyCommandContext.Empty;

    public StatementsVisitor(ExecutionThread executionThread)
    {
        _executionThread = executionThread;
    }

    public CommandContext RunAndReturn(IAstNode node)
    {
        Run(node);
        return CommandContext;
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        if (node is ProgramNode programNode)
        {
            foreach (var statementNode in programNode.Statements)
            {
                statementNode.Accept(this);
            }
        }
        else if (node is StatementNode statementNode)
        {
            statementNode.Accept(this);
        }
    }

    /// <inheritdoc />
    public override void Visit(SelectStatementNode node)
    {
        CommandContext = new SelectCommand().Execute(_executionThread, node);
    }

    /// <inheritdoc />
    public override void Visit(ExpressionStatementNode node)
    {
        new ResolveTypesVisitor(_executionThread).Run(node);
        var func = new CreateDelegateVisitor(_executionThread).RunAndReturn(node.ExpressionNode);
        CommandContext = new FunctionCommandContext(func);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallStatementNode node)
    {
        new ResolveTypesVisitor(_executionThread).Run(node);
        var func = new CreateDelegateVisitor(_executionThread).RunAndReturn(node.FunctionNode);
        CommandContext = new FunctionCommandContext(func);
    }
}
