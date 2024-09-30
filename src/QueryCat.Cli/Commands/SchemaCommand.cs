using System.CommandLine;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Functions;
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
            var root = applicationOptions.CreateStdoutApplicationRoot();
            var thread = root.Thread;
            thread.StatementExecuted += (_, threadArgs) =>
            {
                var result = thread.LastResult;
                if (!result.IsNull && result.Type == DataType.Object
                    && result.AsObject is IRowsSchema rowsSchema)
                {
                    threadArgs.ContinueExecution = false;
                    var schema = thread.CallFunction(InfoFunctions.Schema, rowsSchema);
                    thread.TopScope.Variables["result"] = schema;
                    thread.Run("result");
                }
                else
                {
                    Console.Error.WriteLine("Incorrect SQL expression.");
                }
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
