using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Storage;

namespace QueryCat.Cli;

#if ENABLE_PLUGINS
[Command("list", Description = "List all available plugins.")]
public class PluginListCommand : BasePluginCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        base.OnExecute(app, console);

        var executionThread = CreateExecutionThread(new ExecutionOptions
        {
            PagingSize = -1
        });
        var plugins = FunctionsManager.Call(InfoFunctions.Plugins);
        executionThread.Options.DefaultRowsOutput.Write(plugins);
        return 1;
    }
}
#endif
