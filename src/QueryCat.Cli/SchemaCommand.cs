using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Cli;

[Command("schema", Description = "Show query schema.")]
public class SchemaCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        PreInitialize();

        var runner = CreateRunner(new ExecutionOptions
        {
            PagingSize = -1
        });
        runner.ExecutionThread.AfterStatementExecute += (sender, args) =>
        {
            var result = runner.ExecutionThread.LastResult;
            if (!result.IsNull && result.GetInternalType() == DataType.Object
                && result.AsObject is IRowsSchema rowsSchema)
            {
                var schema = FunctionsManager.Call(InfoFunctions.Schema, rowsSchema);
                runner.ExecutionThread.Options.DefaultRowsOutput.Write(schema);
            }
            else
            {
                console.Error.WriteLine("Incorrect SQL expression.");
            }
            args.ContinueExecution = false;
        };
        runner.Run(Query);

        return 0;
    }
}
