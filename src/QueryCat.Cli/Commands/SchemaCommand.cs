using System.CommandLine;
using QueryCat.Backend.Core.Data;
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
            thread.AfterStatementExecute += (_, threadArgs) =>
            {
                var result = thread.LastResult;
                if (!result.IsNull && result.GetInternalType() == DataType.Object
                    && result.AsObject is IRowsSchema rowsSchema)
                {
                    var schema = thread.RunFunction(InfoFunctions.Schema, rowsSchema);
                    var resultIndex = thread.TopScope.GetVariableIndex("result", out var _);
                    if (resultIndex > -1)
                    {
                        thread.TopScope.SetVariable(resultIndex, schema);
                    }
                    else
                    {
                        thread.TopScope.DefineVariable("result", schema);
                    }
                    thread.Run("result");
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
