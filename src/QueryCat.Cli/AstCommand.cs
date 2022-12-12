using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;

namespace QueryCat.Cli;

[Command("ast", Description = "Show query AST (for debug).")]
public class AstCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        base.OnExecute(app, console);

        var executionThread = CreateExecutionThread(new ExecutionOptions());
        executionThread.BeforeStatementExecute += (_, args) =>
        {
            console.WriteLine(executionThread.DumpAst());
            args.ContinueExecution = false;
        };
        RunQuery(executionThread);

        return 0;
    }
}
