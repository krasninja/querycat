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
        this.SetHandler((applicationOptions, query, variables, files) =>
        {
            applicationOptions.InitializeLogger();
            var root = applicationOptions.CreateStdoutApplicationRoot();
            root.Thread.StatementExecuted += (_, args) =>
            {
                var result = root.Thread.LastResult;
                if (result.GetInternalType() == DataType.Object
                    && result.AsObject is IRowsIterator rowsIterator)
                {
                    var stringBuilder = new IndentedStringBuilder();
                    rowsIterator.Explain(stringBuilder);

                    Console.WriteLine(stringBuilder);
                    args.ContinueExecution = false;
                }
            };
            AddVariables(root.Thread, variables);
            RunQuery(root.Thread, query, files, root.CancellationTokenSource.Token);
        },
            new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption),
            QueryArgument,
            VariablesOption,
            FilesOption);
    }
}
