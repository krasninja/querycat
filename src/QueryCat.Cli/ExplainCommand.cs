using McMaster.Extensions.CommandLineUtils;
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

        var runner = CreateRunner();
        runner.ExecutionThread.AfterStatementExecute += (sender, args) =>
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
        runner.Run(Query);

        return 0;
    }
}
