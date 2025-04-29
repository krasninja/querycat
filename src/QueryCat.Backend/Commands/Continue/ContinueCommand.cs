using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Continue;

internal sealed class ContinueCommand : ICommand
{
    private static readonly ExecutionFlowFuncUnit _instance = new(ExecutionJump.Continue);

    /// <inheritdoc />
    public Task<IFuncUnit> CreateHandlerAsync(IExecutionThread<ExecutionOptions> executionThread, StatementNode node,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((IFuncUnit)_instance);
    }
}
