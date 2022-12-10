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

        var runner = CreateRunner(new ExecutionOptions());
        runner.ExecutionThread.BeforeStatementExecute += (_, args) =>
        {
            console.WriteLine(runner.DumpAst());
            args.ContinueExecution = false;
        };
        runner.Run(Query);

        return 0;
    }
}
