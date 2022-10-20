using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Providers;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli;

/// <summary>
/// Base class for CLI commands.
/// </summary>
public abstract class BaseQueryCommand
{
    [Argument(0, Description = "SQL-like query.")]
    public string Query { get; } = string.Empty;

    [Option("--log-level", Description = "Log level.")]
    public LogLevel LogLevel { get; } =
#if DEBUG
        LogLevel.Debug;
#else
        LogLevel.Info;
#endif

    [Option("--plugin-dir", Description = "Plugin directory.")]
    public string[] PluginDirectories { get; } = Array.Empty<string>();

    /// <summary>
    /// Command entry point.
    /// </summary>
    /// <param name="app">Application instance.</param>
    /// <param name="console">Console abstraction.</param>
    /// <returns>Error code.</returns>
    public virtual int OnExecute(CommandLineApplication app, IConsole console)
    {
        return 0;
    }

    /// <summary>
    /// Pre initialization steps.
    /// </summary>
    protected void PreInitialize()
    {
        Logger.Instance.MinLevel = LogLevel;
    }

    protected Runner CreateRunner(ExecutionOptions executionOptions)
    {
        executionOptions.PluginAssemblies.Add(typeof(QueryCat.DataProviders.Registration).Assembly);
        var output = new TextTableOutput(
            stream: StandardInputOutput.GetConsoleOutput(),
            style: executionOptions.OutputStyle);
        executionOptions.DefaultRowsOutput = output;
        var pluginLoader = new PluginsLoader(PluginDirectories);
        executionOptions.PluginAssemblies.AddRange(pluginLoader.LoadPlugins());
        var runner = new Runner(executionOptions);
        runner.ExecutionThread.Statistic.CountErrorRows = runner.ExecutionThread.Options.ShowDetailedStatistic;
        runner.Bootstrap();
        return runner;
    }
}
