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
    public async Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default)
    {
        var setNode = (SetNode)node.RootNode;

        var valueHandler = await new StatementsVisitor(executionThread, _resolveTypesVisitor)
            .RunAndReturnAsync(setNode.ValueNode, cancellationToken);
        var identifierHandler = await new SetIdentifierDelegateVisitor(executionThread, _resolveTypesVisitor, valueHandler)
            .RunAndReturnAsync(setNode.IdentifierNode, cancellationToken);

        IFuncUnit handler = new FuncCommandHandler(async (thread, ct) =>
        {
            await identifierHandler.InvokeAsync(thread, ct);
            return VariantValue.Null;
        });
        return handler;
    }
}
