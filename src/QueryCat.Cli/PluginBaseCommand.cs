using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;

namespace QueryCat.Cli;

#if ENABLE_PLUGINS
public class BasePluginCommand : BaseQueryCommand
{
    [Option("--repo", Description = "Use another repository URI.")]
    public string RepositoryUri { get; } = string.Empty;

    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        base.OnExecute(app, console);
        return 0;
    }

    /// <inheritdoc />
    protected override Runner CreateRunner(ExecutionOptions? executionOptions = null)
    {
        executionOptions ??= new ExecutionOptions
        {
            PluginsRepositoryUri = RepositoryUri
        };
        executionOptions.PluginDirectories.AddRange(PluginDirectories);
        var runner = new Runner(executionOptions);
        runner.Bootstrap();
        return runner;
    }
}
#endif
