using System.CommandLine;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal class ExplainCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public ExplainCommand() : base("explain", "Show query plan for debugging.")
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
            root.Thread.StatementExecuted += (_, args) =>
            {
                var result = root.Thread.LastResult;
                if (result.Type == DataType.Object
                    && result.AsObject is IRowsIterator rowsIterator)
                {
                    var stringBuilder = new IndentedStringBuilder();
                    rowsIterator.Explain(stringBuilder);

                    Console.WriteLine(stringBuilder);
                    args.ContinueExecution = false;
                }
            };
            AddVariables(root.Thread, variables);
            await RunQueryAsync(root.Thread, query, files, root.CancellationTokenSource.Token);
        });
    }
}
