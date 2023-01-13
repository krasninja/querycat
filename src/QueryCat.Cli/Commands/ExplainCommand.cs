using System.CommandLine;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal class ExplainCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public ExplainCommand() : base("explain", "Show query plan for debugging.")
    {
        this.SetHandler((applicationOptions, query, files) =>
        {
            applicationOptions.InitializeLogger();
            var thread = applicationOptions.CreateExecutionThread(new ExecutionOptions
            {
                PagingSize = ExecutionOptions.NoLimit,
            });
            thread.AfterStatementExecute += (_, args) =>
            {
                var result = thread.LastResult;
                if (result.GetInternalType() == DataType.Object
                    && result.AsObject is IRowsIterator rowsIterator)
                {
                    var stringBuilder = new IndentedStringBuilder();
                    rowsIterator.Explain(stringBuilder);

                    Console.WriteLine(stringBuilder);
                    args.ContinueExecution = false;
                }
            };
            RunQuery(thread, query, files);
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), QueryArgument, FilesOption);
    }
}
