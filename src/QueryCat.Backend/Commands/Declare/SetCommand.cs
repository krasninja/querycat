using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Declare;

internal sealed class SetCommand : ICommand
{
    private readonly ResolveTypesVisitor _resolveTypesVisitor;

    public SetCommand(ResolveTypesVisitor resolveTypesVisitor)
    {
        _resolveTypesVisitor = resolveTypesVisitor;
    }

    /// <inheritdoc />
    public Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default)
    {
        var setNode = (SetNode)node.RootNode;

        var valueHandler = new StatementsVisitor(executionThread, _resolveTypesVisitor)
            .RunAndReturn(setNode.ValueNode);
        var identifierHandler = new SetIdentifierDelegateVisitor(executionThread, _resolveTypesVisitor, valueHandler)
            .RunAndReturn(setNode.IdentifierNode);

        IFuncUnit handler = new FuncCommandHandler(async (thread, ct) =>
        {
            await identifierHandler.InvokeAsync(thread, ct);
            return VariantValue.Null;
        });
        return Task.FromResult(handler);
    }
}
