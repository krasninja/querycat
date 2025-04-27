using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Call;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Ast.Nodes.Delete;
using QueryCat.Backend.Ast.Nodes.For;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.If;
using QueryCat.Backend.Ast.Nodes.Insert;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.Update;
using QueryCat.Backend.Commands.Call;
using QueryCat.Backend.Commands.Declare;
using QueryCat.Backend.Commands.Delete;
using QueryCat.Backend.Commands.For;
using QueryCat.Backend.Commands.If;
using QueryCat.Backend.Commands.Insert;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Commands.Update;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Visit only program top-level building blocks (statements) and generate execution delegate.
/// </summary>
internal sealed class StatementsVisitor : CreateDelegateVisitor
{
    private readonly IExecutionThread<ExecutionOptions> _executionThread;

    public StatementsVisitor(IExecutionThread<ExecutionOptions> executionThread, ResolveTypesVisitor resolveTypesVisitor)
        : base(executionThread, resolveTypesVisitor)
    {
        _executionThread = executionThread;
    }

    public StatementsVisitor(IExecutionThread<ExecutionOptions> executionThread)
        : this(executionThread, new ResolveTypesVisitor(executionThread))
    {
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
        var blockHandlers = node.Statements.Select(s => NodeIdFuncMap[s.Id]).ToArray();

        NodeIdFuncMap[node.Id] = new BlockExpressionFuncUnit(blockHandlers);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(ExpressionStatementNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.RunAsync(node, cancellationToken);
        var handler = await new CreateDelegateVisitor(_executionThread, ResolveTypesVisitor)
            .RunAndReturnAsync(node.ExpressionNode, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(CallFunctionStatementNode node, CancellationToken cancellationToken)
    {
        if (!IsAcceptable(node))
        {
            return;
        }
        var handler = await new CallCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(DeclareStatementNode node, CancellationToken cancellationToken)
    {
        if (!IsAcceptable(node))
        {
            return;
        }
        var handler = await new DeclareCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(DeleteStatementNode node, CancellationToken cancellationToken)
    {
        if (!IsAcceptable(node))
        {
            return;
        }
        var handler = await new DeleteCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SetStatementNode node, CancellationToken cancellationToken)
    {
        if (!IsAcceptable(node))
        {
            return;
        }
        var handler = await new SetCommand(ResolveTypesVisitor).CreateHandlerAsync(_executionThread, node, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(FunctionCallStatementNode node, CancellationToken cancellationToken)
    {
        var handler = await new CreateDelegateVisitor(_executionThread, ResolveTypesVisitor)
            .RunAndReturnAsync(node.FunctionNode, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        await ResolveTypesVisitor.RunAsync(node, cancellationToken);
        await base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(IfConditionStatementNode node, CancellationToken cancellationToken)
    {
        if (!IsAcceptable(node))
        {
            return;
        }
        var handler = await new IfConditionCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(InsertStatementNode node, CancellationToken cancellationToken)
    {
        if (!IsAcceptable(node))
        {
            return;
        }
        var handler = await new InsertCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectStatementNode node, CancellationToken cancellationToken)
    {
        if (!IsAcceptable(node))
        {
            return;
        }
        var handler = await new SelectCommand(ResolveTypesVisitor).CreateHandlerAsync(_executionThread, node, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(UpdateStatementNode node, CancellationToken cancellationToken)
    {
        if (!IsAcceptable(node))
        {
            return;
        }
        var handler = await new UpdateCommand().CreateHandlerAsync(_executionThread, node, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(ForStatementNode node, CancellationToken cancellationToken)
    {
        if (!IsAcceptable(node))
        {
            return;
        }
        var handler = await new ForCommand(this).CreateHandlerAsync(_executionThread, node, cancellationToken);
        NodeIdFuncMap.Add(node.Id, handler);
    }

    private sealed class NotAllowedCommandFuncUnit : IFuncUnit
    {
        private readonly string _commandName;

        /// <inheritdoc />
        public DataType OutputType => DataType.Void;

        public NotAllowedCommandFuncUnit(string commandName)
        {
            _commandName = commandName;
        }

        /// <inheritdoc />
        public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            throw new QueryCatException(string.Format(Resources.Errors.CommandNotAllowed, _commandName));
        }
    }

    private bool IsAcceptable(IAstNode node)
    {
        if (_executionThread.Options.AllowedCommands != null
            && node is ICommandNode commandNode
            && !_executionThread.Options.AllowedCommands.Contains(commandNode.CommandName, StringComparer.InvariantCultureIgnoreCase))
        {
            NodeIdFuncMap.Add(node.Id, new NotAllowedCommandFuncUnit(commandNode.CommandName));
            return false;
        }
        return true;
    }
}
