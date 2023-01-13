using System.CommandLine;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Storage;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginListCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginListCommand() : base("list", "List all available plugins.")
    {
        this.SetHandler(queryOptions =>
        {
            queryOptions.InitializeLogger();
            var executionThread = queryOptions.CreateExecutionThread(new ExecutionOptions
            {
                PagingSize = ExecutionOptions.NoLimit
            });
            var result = executionThread.RunFunction(InfoFunctions.Plugins);
            executionThread.Options.DefaultRowsOutput.Write(ExecutionThreadUtils.ConvertToIterator(result));
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption));
    }
}
#endif
