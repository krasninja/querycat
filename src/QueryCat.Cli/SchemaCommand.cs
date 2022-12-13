using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Cli;

[Command("schema", Description = "Show query schema.")]
public class SchemaCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        base.OnExecute(app, console);

        var executionThread = CreateExecutionThread(new ExecutionOptions
        {
            PagingSize = -1
        });
        executionThread.AfterStatementExecute += (_, args) =>
        {
            var result = executionThread.LastResult;
            if (!result.IsNull && result.GetInternalType() == DataType.Object
                && result.AsObject is IRowsSchema rowsSchema)
            {
                var schema = executionThread.RunFunction(InfoFunctions.Schema, rowsSchema);
                executionThread.Options.DefaultRowsOutput.Write(
                    ExecutionThreadUtils.ConvertToIterator(schema));
            }
            else
            {
                console.Error.WriteLine("Incorrect SQL expression.");
            }
            args.ContinueExecution = false;
        };
        RunQuery(executionThread);

        return 0;
    }
}
