using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Commands;

internal static class CommandExtensions
{
    internal static IFuncUnit CreateHandler(
        this ICommand command,
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node)
    {
        return AsyncUtils.RunSync(ct => command.CreateHandlerAsync(executionThread, node, ct))!;
    }
}
