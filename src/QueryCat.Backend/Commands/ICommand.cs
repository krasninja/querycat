using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands;

/// <summary>
/// QueryCat statement command executor.
/// </summary>
internal interface ICommand
{
    /// <summary>
    /// Process the statement node and create execution handler.
    /// </summary>
    /// <param name="executionThread">Current execution thread.</param>
    /// <param name="node">Statement node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="IFuncUnit" />.</returns>
    Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default);
}
