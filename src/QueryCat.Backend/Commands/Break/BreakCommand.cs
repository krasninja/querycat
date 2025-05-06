using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Break;

internal sealed class BreakCommand : ICommand
{
    private static readonly ExecutionFlowFuncUnit _instance = new(ExecutionJump.Break);

    /// <inheritdoc />
    public Task<IFuncUnit> CreateHandlerAsync(IExecutionThread<ExecutionOptions> executionThread, StatementNode node,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((IFuncUnit)_instance);
    }
}
