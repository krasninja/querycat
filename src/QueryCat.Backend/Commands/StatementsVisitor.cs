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
    public override IFuncUnit RunAndReturn(IAstNode node)
    {
        if (_handlers.TryGetValue(node.Id, out var funcUnit))
        {
            return funcUnit;
        }
        Run(node);
        var handler = _handlers[node.Id];
        _handlers.Clear();
        return handler;
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        node.Accept(this);
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

    public override void Visit(BlockExpressionNode node)
    {
        foreach (var statementNode in node.Statements)
        {
            statementNode.Accept(this);
        }
        var blockHandlers = node.Statements.Select(s => _handlers[s.Id]).ToArray();

        _handlers[node.Id] = new BlockExpressionFuncUnit(blockHandlers);
    }

    /// <inheritdoc />
    public override void Visit(ExpressionStatementNode node)
    {
        _resolveTypesVisitor.Run(node);
        var handler = new CreateDelegateVisitor(_executionThread, _resolveTypesVisitor).RunAndReturn(node.ExpressionNode);
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(CallFunctionStatementNode node)
    {
        var handler = new CallCommand().CreateHandlerAsync(_executionThread, node)
            .GetAwaiter().GetResult();
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(DeclareStatementNode node)
    {
        var handler = new DeclareCommand().CreateHandlerAsync(_executionThread, node)
            .GetAwaiter().GetResult();
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(SetStatementNode node)
    {
        var handler = new SetCommand(_resolveTypesVisitor).CreateHandlerAsync(_executionThread, node)
            .GetAwaiter().GetResult();
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallStatementNode node)
    {
        _resolveTypesVisitor.Run(node);
        var handler = new CreateDelegateVisitor(_executionThread, _resolveTypesVisitor).RunAndReturn(node.FunctionNode);
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(IfConditionStatementNode node)
    {
        var handler = new IfConditionCommand().CreateHandlerAsync(_executionThread, node)
            .GetAwaiter().GetResult();
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(InsertStatementNode node)
    {
        var handler = new InsertCommand().CreateHandlerAsync(_executionThread, node)
            .GetAwaiter().GetResult();
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(SelectStatementNode node)
    {
        var handler = new SelectCommand(_resolveTypesVisitor).CreateHandlerAsync(_executionThread, node)
            .GetAwaiter().GetResult();
        _handlers.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override void Visit(UpdateStatementNode node)
    {
        var handler = new UpdateCommand().CreateHandlerAsync(_executionThread, node)
            .GetAwaiter().GetResult();
        _handlers.Add(node.Id, handler);
    }
}
