using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Execution;

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
    /// <returns>Instance of <see cref="CommandHandler" />.</returns>
    CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node);
}
