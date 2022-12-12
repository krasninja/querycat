using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Cli;

[Command("explain", Description = "Show query plan (for debug).")]
public class ExplainCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        base.OnExecute(app, console);

        var runner = CreateRunner(new ExecutionOptions
        {
            PagingSize = -1,
        });
        runner.ExecutionThread.AfterStatementExecute += (_, args) =>
        {
            var result = runner.ExecutionThread.LastResult;
            if (result.GetInternalType() == DataType.Object
                && result.AsObject is IRowsIterator rowsIterator)
            {
                var stringBuilder = new IndentedStringBuilder();
                rowsIterator.Explain(stringBuilder);

                console.WriteLine(stringBuilder);
                args.ContinueExecution = false;
            }
        };
        RunQuery(runner);

        return 0;
    }
}
