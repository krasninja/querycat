using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Insert;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.Update;
using QueryCat.Backend.Commands.Declare;
using QueryCat.Backend.Commands.Insert;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Commands.Update;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Visit only program top-level building blocks (statements) and generate execution delegate.
/// </summary>
public sealed class StatementsVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;

    public CommandHandler CommandContext { get; private set; } = EmptyCommandHandler.Empty;

    public StatementsVisitor(ExecutionThread executionThread)
    {
        _executionThread = executionThread;
    }

    public CommandHandler RunAndReturn(IAstNode node)
    {
        Run(node);
        return CommandContext;
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        node.Accept(this);
    }

    /// <inheritdoc />
    public override void Visit(SelectStatementNode node)
    {
        CommandContext = new SelectCommand().CreateHandler(_executionThread, node);
    }

    /// <inheritdoc />
    public override void Visit(InsertStatementNode node)
    {
        CommandContext = new InsertCommand().CreateHandler(_executionThread, node);
    }

    /// <inheritdoc />
    public override void Visit(UpdateStatementNode node)
    {
        CommandContext = new UpdateCommand().CreateHandler(_executionThread, node);
    }

    /// <inheritdoc />
    public override void Visit(ExpressionStatementNode node)
    {
        new ResolveTypesVisitor(_executionThread).Run(node);
        var func = new CreateDelegateVisitor(_executionThread).RunAndReturn(node.ExpressionNode);
        CommandContext = new FuncUnitCommandHandler(func);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallStatementNode node)
    {
        new ResolveTypesVisitor(_executionThread).Run(node);
        var func = new CreateDelegateVisitor(_executionThread).RunAndReturn(node.FunctionNode);
        CommandContext = new FuncUnitCommandHandler(func);
    }

    /// <inheritdoc />
    public override void Visit(DeclareStatementNode node)
    {
        CommandContext = new DeclareCommand().CreateHandler(_executionThread, node);
    }

    /// <inheritdoc />
    public override void Visit(SetStatementNode node)
    {
        CommandContext = new SetCommand().CreateHandler(_executionThread, node);
    }
}
