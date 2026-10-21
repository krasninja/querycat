using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Call;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Call;

internal sealed class CallCommand : ICommand
{
    /// <inheritdoc />
    public async Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default)
    {
        var callFunctionNode = (CallFunctionNode)node.RootNode;
        var handler = await new CreateDelegateVisitor(executionThread)
            .RunAndReturnAsync(callFunctionNode.FunctionCallNode, cancellationToken);
        return handler;
    }
}
