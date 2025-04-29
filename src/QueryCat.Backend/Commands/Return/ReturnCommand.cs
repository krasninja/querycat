using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Return;

internal sealed class ReturnCommand : ICommand
{
    private static readonly ExecutionFlowFuncUnit _instance = new(ExecutionJump.Return);

    /// <inheritdoc />
    public Task<IFuncUnit> CreateHandlerAsync(IExecutionThread<ExecutionOptions> executionThread, StatementNode node,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((IFuncUnit)_instance);
    }
}
