using System.CommandLine;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Functions;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal class SchemaCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public SchemaCommand() : base("schema", "Show query result columns.")
    {
        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var query = OptionsUtils.GetValueForOption(QueryArgument, context);
            var variables = OptionsUtils.GetValueForOption(VariablesOption, context);
            var files = OptionsUtils.GetValueForOption(FilesOption, context);

            applicationOptions.InitializeLogger();
            var root = await applicationOptions.CreateStdoutApplicationRootAsync();
            var thread = root.Thread;
            thread.StatementExecuted += async (_, args) =>
            {
                if (!args.Result.IsNull
                    && args.Result.Type == DataType.Object
                    && args.Result.AsObject is IRowsSchema rowsSchema)
                {
                    args.ContinueExecution = false;
                    var schema = await FunctionCaller.CallWithArgumentsAsync(InfoFunctions.Schema, thread, [rowsSchema]);
                    thread.TopScope.Variables["result"] = schema;
                    await thread.RunAsync("result", cancellationToken: context.GetCancellationToken());
                }
            };
            AddVariables(thread, variables);
            await RunQueryAsync(thread, query, files, root.CancellationTokenSource.Token);
        });
    }
}
