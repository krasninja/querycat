using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Declare;

internal sealed class SetCommand : ICommand
{
    /// <inheritdoc />
    public IFuncUnit CreateHandler(IExecutionThread<ExecutionOptions> executionThread, StatementNode node)
    {
        var setNode = (SetNode)node.RootNode;

        var valueHandler = new StatementsVisitor(executionThread)
            .RunAndReturn(setNode.ValueNode);
        var identifierHandler = new SetIdentifierDelegateVisitor(executionThread, valueHandler)
            .RunAndReturn(setNode.IdentifierNode);

        return new FuncCommandHandler(() =>
        {
            identifierHandler.Invoke();
            return VariantValue.Null;
        });
    }
}
