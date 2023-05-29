using System.CommandLine;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal class SchemaCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public SchemaCommand() : base("schema", "Show query result columns.")
    {
        this.SetHandler((applicationOptions, query, variables, files) =>
        {
            applicationOptions.InitializeLogger();
            var thread = applicationOptions.CreateStdoutExecutionThread();
            thread.AfterStatementExecute += (_, threadArgs) =>
            {
                var result = thread.LastResult;
                if (!result.IsNull && result.GetInternalType() == DataType.Object
                    && result.AsObject is IRowsSchema rowsSchema)
                {
                    var schema = thread.RunFunction(InfoFunctions.Schema, rowsSchema);
                    thread.Options.DefaultRowsOutput.Write(ExecutionThreadUtils.ConvertToIterator(schema), thread);
                }
                else
                {
                    Console.Error.WriteLine("Incorrect SQL expression.");
                }
                threadArgs.ContinueExecution = false;
            };
            AddVariables(thread, variables);
            RunQuery(thread, query, files);
        },
            new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption),
            QueryArgument,
            VariablesOption,
            FilesOption);
    }
}
