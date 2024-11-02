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
    public IFuncUnit CreateHandler(IExecutionThread<ExecutionOptions> executionThread, StatementNode node)
    {
        var setNode = (SetNode)node.RootNode;

        var valueHandler = new StatementsVisitor(executionThread, _resolveTypesVisitor)
            .RunAndReturn(setNode.ValueNode);
        var identifierHandler = new SetIdentifierDelegateVisitor(executionThread, _resolveTypesVisitor, valueHandler)
            .RunAndReturn(setNode.IdentifierNode);

        return new FuncCommandHandler(thread =>
        {
            identifierHandler.Invoke(thread);
            return VariantValue.Null;
        });
    }
}
