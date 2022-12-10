using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Logging;

namespace QueryCat.Cli;

/// <summary>
/// Base class for CLI commands.
/// </summary>
public abstract class BaseQueryCommand
{
    [Argument(0, Description = "SQL-like query or command argument.")]
    public string Query { get; private set; } = string.Empty;

    [Option("-f|--files", Description = "SQL files.")]
    public List<string> Files { get; } = new();

    [Option("--log-level", Description = "Log level.")]
    public LogLevel LogLevel { get; } =
#if DEBUG
        LogLevel.Debug;
#else
        LogLevel.Info;
#endif

#if ENABLE_PLUGINS
    [Option("--plugin-dir", Description = "Plugin directory.")]
    public string[] PluginDirectories { get; } = Array.Empty<string>();
#endif

    /// <summary>
    /// Command entry point.
    /// </summary>
    /// <param name="app">Application instance.</param>
    /// <param name="console">Console abstraction.</param>
    /// <returns>Error code.</returns>
    public virtual int OnExecute(CommandLineApplication app, IConsole console)
    {
        PreInitialize();
        return 0;
    }

    /// <summary>
    /// Pre initialization steps.
    /// </summary>
    private void PreInitialize()
    {
        Logger.Instance.MinLevel = LogLevel;

        if (Files.Any())
        {
            Query = string.Join(Environment.NewLine, Files.Select(File.ReadAllText));
        }
    }

    protected virtual Runner CreateRunner(ExecutionOptions? executionOptions = null)
    {
        executionOptions ??= new ExecutionOptions();
#if ENABLE_PLUGINS
        executionOptions.PluginDirectories.AddRange(PluginDirectories);
#endif
        var runner = new Runner(executionOptions);
        runner.ExecutionThread.Statistic.CountErrorRows = runner.ExecutionThread.Options.ShowDetailedStatistic;
        runner.Bootstrap();
        return runner;
    }
}
