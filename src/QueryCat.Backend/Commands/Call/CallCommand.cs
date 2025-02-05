using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Call;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Call;

internal sealed class CallCommand : ICommand
{
    /// <inheritdoc />
    public Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default)
    {
        var declareNode = (CallFunctionNode)node.RootNode;
        var handler = new CreateDelegateVisitor(executionThread).RunAndReturn(declareNode.FunctionCallNode);
        return Task.FromResult(handler);
    }
}
