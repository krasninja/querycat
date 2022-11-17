using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Storage;

namespace QueryCat.Cli;

[Command("list", Description = "List all available plugins.")]
public class PluginListCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        var runner = CreateRunner(new ExecutionOptions
        {
            PagingSize = -1
        });
        var plugins = FunctionsManager.Call(InfoFunctions.Plugins);
        runner.ExecutionThread.Options.DefaultRowsOutput.Write(plugins);
        return 1;
    }
}
