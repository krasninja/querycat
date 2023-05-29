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
    private readonly Dictionary<int, CommandHandler> _commandHandlers = new();

    public StatementsVisitor(ExecutionThread executionThread)
    {
        _executionThread = executionThread;
    }

    public CommandHandler RunAndReturn(IAstNode node)
    {
        if (_commandHandlers.TryGetValue(node.Id, out var funcUnit))
        {
            return funcUnit;
        }
        Run(node);
        return _commandHandlers[node.Id];
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        node.Accept(this);
    }

    /// <inheritdoc />
    public override void Visit(SelectStatementNode node)
    {
        var handler = new SelectCommand().CreateHandler(_executionThread, node);
        _commandHandlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(InsertStatementNode node)
    {
        var handler = new InsertCommand().CreateHandler(_executionThread, node);
        _commandHandlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(UpdateStatementNode node)
    {
        var handler = new UpdateCommand().CreateHandler(_executionThread, node);
        _commandHandlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(ExpressionStatementNode node)
    {
        new ResolveTypesVisitor(_executionThread).Run(node);
        var func = new CreateDelegateVisitor(_executionThread).RunAndReturn(node.ExpressionNode);
        var handler = new FuncUnitCommandHandler(func);
        _commandHandlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallStatementNode node)
    {
        new ResolveTypesVisitor(_executionThread).Run(node);
        var func = new CreateDelegateVisitor(_executionThread).RunAndReturn(node.FunctionNode);
        var handler = new FuncUnitCommandHandler(func);
        _commandHandlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(DeclareStatementNode node)
    {
        var handler = new DeclareCommand().CreateHandler(_executionThread, node);
        _commandHandlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(SetStatementNode node)
    {
        var handler = new SetCommand().CreateHandler(_executionThread, node);
        _commandHandlers.Add(node.Id, handler);
    }
}
