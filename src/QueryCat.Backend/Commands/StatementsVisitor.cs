using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Call;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.If;
using QueryCat.Backend.Ast.Nodes.Insert;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.Update;
using QueryCat.Backend.Commands.Call;
using QueryCat.Backend.Commands.Declare;
using QueryCat.Backend.Commands.If;
using QueryCat.Backend.Commands.Insert;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Commands.Update;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Visit only program top-level building blocks (statements) and generate execution delegate.
/// </summary>
internal sealed class StatementsVisitor : AstVisitor
{
    private readonly IExecutionThread<ExecutionOptions> _executionThread;
    private readonly Dictionary<int, IFuncUnit> _handlers = new();
    private readonly ResolveTypesVisitor _resolveTypesVisitor;

    public StatementsVisitor(IExecutionThread<ExecutionOptions> executionThread, ResolveTypesVisitor resolveTypesVisitor)
    {
        _executionThread = executionThread;
        _resolveTypesVisitor = resolveTypesVisitor;
    }

    public StatementsVisitor(IExecutionThread<ExecutionOptions> executionThread)
        : this(executionThread, new ResolveTypesVisitor(executionThread))
    {
    }

    /// <inheritdoc />
    public override async ValueTask<IFuncUnit> RunAndReturnAsync(IAstNode node, CancellationToken cancellationToken)
    {
        if (_handlers.TryGetValue(node.Id, out var funcUnit))
        {
            return funcUnit;
        }
        await RunAsync(node, cancellationToken);
        var handler = _handlers[node.Id];
        _handlers.Clear();
        return handler;
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        node.Accept(this);
    }

    /// <inheritdoc />
    public override ValueTask RunAsync(IAstNode node, CancellationToken cancellationToken)
    {
        return node.AcceptAsync(this, cancellationToken);
    }

    private sealed class BlockExpressionFuncUnit(IFuncUnit[] blocks) : IFuncUnit
    {
        /// <inheritdoc />
        public DataType OutputType => blocks[^1].OutputType;

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            var result = VariantValue.Null;
            foreach (var func in blocks)
            {
                result = await func.InvokeAsync(thread, cancellationToken);
            }
            return result;
        }
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(BlockExpressionNode node, CancellationToken cancellationToken)
    {
        foreach (var statementNode in node.Statements)
        {
            await statementNode.AcceptAsync(this, cancellationToken);
        }
        var blockHandlers = node.Statements.Select(s => _handlers[s.Id]).ToArray();

        _handlers[node.Id] = new BlockExpressionFuncUnit(blockHandlers);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(ExpressionStatementNode node, CancellationToken cancellationToken)
    {
        await _resolveTypesVisitor.RunAsync(node, cancellationToken);
        var handler = await new CreateDelegateVisitor(_executionThread, _resolveTypesVisitor)
            .RunAndReturnAsync(node.ExpressionNode, cancellationToken);
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(CallFunctionStatementNode node, CancellationToken cancellationToken)
    {
        var handler = await new CallCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(DeclareStatementNode node, CancellationToken cancellationToken)
    {
        var handler = await new DeclareCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SetStatementNode node, CancellationToken cancellationToken)
    {
        var handler = await new SetCommand(_resolveTypesVisitor).CreateHandlerAsync(_executionThread, node, cancellationToken);
        _handlers.Add(node.Id, handler);
    }

    public override async ValueTask VisitAsync(FunctionCallStatementNode node, CancellationToken cancellationToken)
    {
        await _resolveTypesVisitor.RunAsync(node, cancellationToken);
        var handler = await new CreateDelegateVisitor(_executionThread, _resolveTypesVisitor)
            .RunAndReturnAsync(node.FunctionNode, cancellationToken);
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(IfConditionStatementNode node, CancellationToken cancellationToken)
    {
        var handler = await new IfConditionCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(InsertStatementNode node, CancellationToken cancellationToken)
    {
        var handler = await new InsertCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectStatementNode node, CancellationToken cancellationToken)
    {
        var handler = await new SelectCommand(_resolveTypesVisitor).CreateHandlerAsync(_executionThread, node, cancellationToken);
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(UpdateStatementNode node, CancellationToken cancellationToken)
    {
        var handler = await new UpdateCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        _handlers.Add(node.Id, handler);
    }
}
