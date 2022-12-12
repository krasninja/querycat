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

        var executionThread = CreateExecutionThread(new ExecutionOptions
        {
            PagingSize = -1,
        });
        executionThread.AfterStatementExecute += (_, args) =>
        {
            var result = executionThread.LastResult;
            if (result.GetInternalType() == DataType.Object
                && result.AsObject is IRowsIterator rowsIterator)
            {
                var stringBuilder = new IndentedStringBuilder();
                rowsIterator.Explain(stringBuilder);

                console.WriteLine(stringBuilder);
                args.ContinueExecution = false;
            }
        };
        RunQuery(executionThread);

        return 0;
    }
}
